using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IDesktopHubConnection
    {
        bool IsConnected { get; }

        Task<bool> Connect(CancellationToken cancellationToken);
        Task Disconnect();
        Task DisconnectAllViewers();
        Task DisconnectViewer(Viewer viewer, bool notifyViewer);
        Task<string> GetSessionID();
        Task NotifyRequesterUnattendedReady(string requesterID);
        Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs);
        Task SendConnectionFailedToViewers(List<string> viewerIDs);
        Task SendConnectionRequestDenied(string viewerID);
        Task SendCtrlAltDelToAgent();
        Task SendDeviceInfo(string serviceID, string machineName, string deviceID);
        Task SendDtoToViewer<T>(T dto, string viewerId);
        Task SendMessageToViewer(string viewerID, string message);
        Task SendViewerConnected(string viewerConnectionId);
    }

    public class DesktopHubConnection : IDesktopHubConnection
    {
        private readonly IIdleTimer _idleTimer;

        private readonly IDtoMessageHandler _messageHandler;

        private readonly IRemoteControlAccessService _remoteControlAccessService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAppState _appState;
        private readonly ILogger<DesktopHubConnection> _logger;
        private readonly IScreenCaster _screenCaster;

        private HubConnection _connection;


        public DesktopHubConnection(
            IIdleTimer idleTimer,
            IDtoMessageHandler messageHandler,
            IScreenCaster screenCastService,
            IServiceScopeFactory scopeFactory,
            IAppState appState,
            IRemoteControlAccessService remoteControlAccessService,
            ILogger<DesktopHubConnection> logger)
        {
            _idleTimer = idleTimer;
            _messageHandler = messageHandler;
            _screenCaster = screenCastService;
            _remoteControlAccessService = remoteControlAccessService;
            _scopeFactory = scopeFactory;
            _appState = appState;
            _logger = logger;


            _connection = BuildConnection();
        }

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;
        public async Task<bool> Connect(CancellationToken cancellationToken)
        {
            try
            {
                if (_connection is not null &&
                    _connection.State != HubConnectionState.Disconnected)
                {
                    return true;
                }

                _connection = BuildConnection();

                ApplyConnectionHandlers();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Connecting to server.");

                        await _connection.StartAsync(cancellationToken);

                        _logger.LogInformation("Connected to server.");

                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning("Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in hub connection.");
                    }
                    await Task.Delay(3_000, cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while connecting to hub.");
                return false;
            }
        }

        private HubConnection BuildConnection()
        {
            using var scope = _scopeFactory.CreateScope();
            var builder = scope.ServiceProvider.GetRequiredService<IHubConnectionBuilder>();

            var connection = builder
                .WithUrl($"{_appState.Host.Trim().TrimEnd('/')}/hubs/desktop")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
            return connection;
        }

        public async Task Disconnect()
        {
            try
            {
                if (_connection is not null)
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting websocket.");
            }
        }

        public async Task DisconnectAllViewers()
        {
            foreach (var viewer in _appState.Viewers.Values.ToList())
            {
                await DisconnectViewer(viewer, true);
            }
        }

        public Task DisconnectViewer(Viewer viewer, bool notifyViewer)
        {
            viewer.DisconnectRequested = true;
            viewer.Dispose();
            return _connection.SendAsync("DisconnectViewer", viewer.ViewerConnectionID, notifyViewer);
        }

        public async Task<string> GetSessionID()
        {
            return await _connection.InvokeAsync<string>("GetSessionID");
        }

        public Task NotifyRequesterUnattendedReady(string requesterID)
        {
            return _connection.SendAsync("NotifyRequesterUnattendedReady", requesterID);
        }

        public Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs)
        {
            return _connection.SendAsync("NotifyViewersRelaunchedScreenCasterReady", viewerIDs);
        }

        public Task SendConnectionFailedToViewers(List<string> viewerIDs)
        {
            return _connection.SendAsync("SendConnectionFailedToViewers", viewerIDs);
        }

        public Task SendConnectionRequestDenied(string viewerID)
        {
            return _connection.SendAsync("SendConnectionRequestDenied", viewerID);
        }

        public Task SendCtrlAltDelToAgent()
        {
            return _connection.SendAsync("SendCtrlAltDelToAgent");
        }

        public Task SendDeviceInfo(string serviceID, string machineName, string deviceID)
        {
            return _connection.SendAsync("ReceiveDeviceInfo", serviceID, machineName, deviceID);
        }

        public Task SendDtoToViewer<T>(T dto, string viewerId)
        {
            var serializedDto = MessagePack.MessagePackSerializer.Serialize(dto);
            return _connection.SendAsync("SendDtoToBrowser", serializedDto, viewerId);
        }

        public Task SendMessageToViewer(string viewerID, string message)
        {
            return _connection.SendAsync("SendMessageToViewer", viewerID, message);
        }

        public Task SendViewerConnected(string viewerConnectionId)
        {
            return _connection.SendAsync("ViewerConnected", viewerConnectionId);
        }

        private void ApplyConnectionHandlers()
        {
            _connection.Closed += (ex) =>
            {
                _logger.LogWarning(ex, "Connection closed.");
                return Task.CompletedTask;
            };

            _connection.On("Disconnect", async (string reason) =>
            {
                _logger.LogInformation("Disconnecting caster socket.  Reason: {reason}", reason);
                await DisconnectAllViewers();
            });

            _connection.On("GetScreenCast", async (
                string viewerID,
                string requesterName,
                bool notifyUser,
                bool enforceAttendedAccess,
                string organizationName) =>
            {
                try
                {
                    if (enforceAttendedAccess)
                    {
                        await SendMessageToViewer(viewerID, "Asking user for permission");

                        _idleTimer.Stop();
                        var result = await _remoteControlAccessService.PromptForAccess(requesterName, organizationName);
                        _idleTimer.Start();

                        if (!result)
                        {
                            await SendConnectionRequestDenied(viewerID);
                            return;
                        }
                    }

                    _screenCaster.BeginScreenCasting(new ScreenCastRequest()
                    {
                        NotifyUser = notifyUser,
                        ViewerID = viewerID,
                        RequesterName = requesterName
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while applying connection handlers.");
                }
            });


            _connection.On("RequestScreenCast", (string viewerID, string requesterName, bool notifyUser) =>
            {
                _appState.InvokeScreenCastRequested(new ScreenCastRequest()
                {
                    NotifyUser = notifyUser,
                    ViewerID = viewerID,
                    RequesterName = requesterName
                });
            });

            _connection.On("SendDtoToClient", (byte[] baseDto, string viewerConnectionId) =>
            {
                if (_appState.Viewers.TryGetValue(viewerConnectionId, out var viewer))
                {
                    _messageHandler.ParseMessage(viewer, baseDto);
                }
            });

            _connection.On("ViewerDisconnected", async (string viewerID) =>
            {
                await _connection.SendAsync("DisconnectViewer", viewerID, false);
                if (_appState.Viewers.TryGetValue(viewerID, out var viewer))
                {
                    viewer.DisconnectRequested = true;
                    viewer.Dispose();
                }
                _appState.InvokeViewerRemoved(viewerID);

            });
        }

        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return TimeSpan.FromSeconds(3);
            }
        }
    }
}
