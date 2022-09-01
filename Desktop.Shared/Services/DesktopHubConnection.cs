using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IDesktopHubConnection
    {
        HubConnection Connection { get; }
        bool IsConnected { get; }

        Task<bool> Connect(CancellationToken cancellationToken, TimeSpan timeout);
        Task Disconnect();
        Task DisconnectAllViewers();
        Task DisconnectViewer(IViewer viewer, bool notifyViewer);
        Task<string> GetSessionID();
        Task NotifyRequesterUnattendedReady();
        Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs);
        Task SendAttendedSessionInfo(string machineName);

        Task SendConnectionFailedToViewers(List<string> viewerIDs);
        Task SendConnectionRequestDenied(string viewerID);
        Task SendDtoToViewer<T>(T dto, string viewerId);

        Task SendMessageToViewer(string viewerID, string message);

        Task<Result> SendUnattendedSessionInfo(string sessionId, string accessKey, string machineName, string requesterName, string organizationName);
        Task SendViewerConnected(string viewerConnectionId);
    }

    public class DesktopHubConnection : IDesktopHubConnection
    {
        private readonly IAppState _appState;
        private readonly IIdleTimer _idleTimer;

        private readonly ILogger<DesktopHubConnection> _logger;
        private readonly IDtoMessageHandler _messageHandler;
        private readonly IRemoteControlAccessService _remoteControlAccessService;
        private readonly IServiceScopeFactory _scopeFactory;

        public DesktopHubConnection(
            IIdleTimer idleTimer,
            IDtoMessageHandler messageHandler,
            IServiceScopeFactory scopeFactory,
            IAppState appState,
            IRemoteControlAccessService remoteControlAccessService,
            ILogger<DesktopHubConnection> logger)
        {
            _idleTimer = idleTimer;
            _messageHandler = messageHandler;
            _remoteControlAccessService = remoteControlAccessService;
            _scopeFactory = scopeFactory;
            _appState = appState;
            _logger = logger;


            Connection = BuildConnection();
        }

        public HubConnection Connection { get; private set; }
        public bool IsConnected => Connection?.State == HubConnectionState.Connected;
        public async Task<bool> Connect(CancellationToken cancellationToken, TimeSpan timeout)
        {
            try
            {
                if (Connection is not null &&
                    Connection.State != HubConnectionState.Disconnected)
                {
                    return true;
                }

                Connection = BuildConnection();

                ApplyConnectionHandlers();

                var sw = Stopwatch.StartNew();
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Connecting to server.");

                        await Connection.StartAsync(cancellationToken);

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

                    if (sw.Elapsed > timeout)
                    {
                        _logger.LogWarning("Timed out while trying to connect to desktop hub.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while connecting to hub.");
                return false;
            }
        }

        public async Task Disconnect()
        {
            try
            {
                if (Connection is not null)
                {
                    await Connection.StopAsync();
                    await Connection.DisposeAsync();
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

        public Task DisconnectViewer(IViewer viewer, bool notifyViewer)
        {
            viewer.DisconnectRequested = true;
            viewer.Dispose();
            return Connection.SendAsync("DisconnectViewer", viewer.ViewerConnectionID, notifyViewer);
        }

        public async Task<string> GetSessionID()
        {
            return await Connection.InvokeAsync<string>("GetSessionID");
        }

        public Task NotifyRequesterUnattendedReady()
        {
            return Connection.SendAsync("NotifyRequesterUnattendedReady");
        }

        public Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs)
        {
            return Connection.SendAsync("NotifyViewersRelaunchedScreenCasterReady", viewerIDs);
        }

        public Task SendAttendedSessionInfo(string machineName)
        {
            return Connection.InvokeAsync("ReceiveAttendedSessionInfo", machineName);
        }

        public Task SendConnectionFailedToViewers(List<string> viewerIDs)
        {
            return Connection.SendAsync("SendConnectionFailedToViewers", viewerIDs);
        }

        public Task SendConnectionRequestDenied(string viewerID)
        {
            return Connection.SendAsync("SendConnectionRequestDenied", viewerID);
        }

        public Task SendDtoToViewer<T>(T dto, string viewerId)
        {
            var serializedDto = MessagePack.MessagePackSerializer.Serialize(dto);
            return Connection.SendAsync("SendDtoToViewer", serializedDto, viewerId);
        }

        public Task SendMessageToViewer(string viewerID, string message)
        {
            return Connection.SendAsync("SendMessageToViewer", viewerID, message);
        }

        public Task<Result> SendUnattendedSessionInfo(string unattendedSessionId, string accessKey, string machineName, string requesterName, string organizationName)
        {
            return Connection.InvokeAsync<Result>("ReceiveUnattendedSessionInfo", unattendedSessionId, accessKey, machineName, requesterName, organizationName);
        }
        public Task SendViewerConnected(string viewerConnectionId)
        {
            return Connection.SendAsync("ViewerConnected", viewerConnectionId);
        }

        private void ApplyConnectionHandlers()
        {
            Connection.Closed += (ex) =>
            {
                _logger.LogWarning(ex, "Connection closed.");
                return Task.CompletedTask;
            };

            Connection.On("Disconnect", async (string reason) =>
            {
                _logger.LogInformation("Disconnecting caster socket.  Reason: {reason}", reason);
                await DisconnectAllViewers();
            });

            Connection.On("GetScreenCast", async (
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

                    using var scope = _scopeFactory.CreateScope();
                    var screenCaster = scope.ServiceProvider.GetRequiredService<IScreenCaster>();

                    screenCaster.BeginScreenCasting(new ScreenCastRequest()
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


            Connection.On("RequestScreenCast", (string viewerID, string requesterName, bool notifyUser) =>
            {
                _appState.InvokeScreenCastRequested(new ScreenCastRequest()
                {
                    NotifyUser = notifyUser,
                    ViewerID = viewerID,
                    RequesterName = requesterName
                });
            });

            Connection.On("SendDtoToClient", (byte[] dtoWrapper, string viewerConnectionId) =>
            {
                if (_appState.Viewers.TryGetValue(viewerConnectionId, out var viewer))
                {
                    _messageHandler.ParseMessage(viewer, dtoWrapper);
                }
            });

            Connection.On("ViewerDisconnected", async (string viewerID) =>
            {
                await Connection.SendAsync("DisconnectViewer", viewerID, false);
                if (_appState.Viewers.TryGetValue(viewerID, out var viewer))
                {
                    viewer.DisconnectRequested = true;
                    viewer.Dispose();
                }
                _appState.InvokeViewerRemoved(viewerID);

            });
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
        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return TimeSpan.FromSeconds(3);
            }
        }
    }
}
