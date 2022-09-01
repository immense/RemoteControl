using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Hubs
{
    public class DesktopHub : Hub
    {
        private readonly IHubEventHandler _hubEvents;
        private readonly ILogger<DesktopHub> _logger;
        private readonly IDesktopHubSessionCache _sessionCache;
        private readonly IHubContext<ViewerHub> _viewerHub;
        public DesktopHub(
            IDesktopHubSessionCache sessionCache,
            IHubContext<ViewerHub> viewerHubContext,
            IHubEventHandler hubEvents,
            ILogger<DesktopHub> logger)
        {
            _sessionCache = sessionCache;
            _viewerHub = viewerHubContext;
            _hubEvents = hubEvents;
            _logger = logger;
        }

      

        private RemoteControlSession SessionInfo
        {
            get
            {
                if (Context.Items.TryGetValue(nameof(SessionInfo), out var result) &&
                    result is RemoteControlSession session)
                {
                    return session;
                }

                var newSession = new RemoteControlSession();
                Context.Items[nameof(SessionInfo)] = newSession;
                return newSession;
            }
        }


        private HashSet<string> ViewerList => SessionInfo.ViewerList;

        public async Task DisconnectViewer(string viewerID, bool notifyViewer)
        {
            ViewerList.Remove(viewerID);

            if (notifyViewer)
            {
                await _viewerHub.Clients.Client(viewerID).SendAsync("ViewerRemoved");
            }
        }

        public string GetSessionID()
        {
            using var scope = _logger.BeginScope(nameof(GetSessionID));

            var random = new Random();
            var sessionId = "";

            while (string.IsNullOrWhiteSpace(sessionId) || _sessionCache.Sessions.ContainsKey(sessionId))
            {
                for (var i = 0; i < 3; i++)
                {
                    sessionId += random.Next(0, 999).ToString().PadLeft(3, '0');
                }
            }

            if (!_sessionCache.Sessions.TryGetValue(Context.ConnectionId, out var session))
            {
                _logger.LogError("Connection not found in cache.");
                return string.Empty;
            }

            session.AttendedSessionID = sessionId;
            return sessionId;
        }

        public async Task NotifyRequesterUnattendedReady(string userConnectionId)
        {
            using var scope = _logger.BeginScope(nameof(NotifyRequesterUnattendedReady));

            if (!_sessionCache.Sessions.TryGetValue(Context.ConnectionId, out var session))
            {
                _logger.LogError("Connection not found in cache.");
                return;
            }

            await _hubEvents.NotifyUnattendedSessionReady(userConnectionId, Context.ConnectionId, session.DeviceID);
        }

        public Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs)
        {
            return _viewerHub.Clients.Clients(viewerIDs).SendAsync("RelaunchedScreenCasterReady", Context.ConnectionId);
        }

        public override async Task OnConnectedAsync()
        {
            SessionInfo.CasterConnectionId = Context.ConnectionId;
            SessionInfo.StartTime = DateTimeOffset.Now;
            _sessionCache.Sessions.AddOrUpdate(Context.ConnectionId, SessionInfo, (id, si) => SessionInfo);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _sessionCache.Sessions.TryRemove(Context.ConnectionId, out _);

            if (SessionInfo.Mode == RemoteControlMode.Attended)
            {
                await _viewerHub.Clients.Clients(ViewerList).SendAsync("ScreenCasterDisconnected");
            }
            else if (SessionInfo.Mode == RemoteControlMode.Unattended)
            {
                if (ViewerList.Count > 0)
                {
                    await _viewerHub.Clients.Clients(ViewerList).SendAsync("Reconnecting");
                    await _hubEvents.RestartScreenCaster(Context.ConnectionId, SessionInfo.ServiceID, ViewerList);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task ReceiveDeviceInfo(string serviceID, string machineName, string deviceID)
        {
            SessionInfo.ServiceID = serviceID;
            SessionInfo.MachineName = machineName;
            SessionInfo.DeviceID = deviceID;
            return Task.CompletedTask;
        }

        public Task SendConnectionFailedToViewers(List<string> viewerIDs)
        {
            return _viewerHub.Clients.Clients(viewerIDs).SendAsync("ConnectionFailed");
        }

        public Task SendConnectionRequestDenied(string viewerID)
        {
            return _viewerHub.Clients.Client(viewerID).SendAsync("ConnectionRequestDenied");
        }

        public Task SendDtoToViewer(byte[] dto, string viewerId)
        {
            return _viewerHub.Clients.Client(viewerId).SendAsync("SendDtoToViewer", dto);
        }

        public Task SendMessageToViewer(string viewerId, string message)
        {
            return _viewerHub.Clients.Client(viewerId).SendAsync("ShowMessage", message);
        }
        public Task ViewerConnected(string viewerConnectionId)
        {
            ViewerList.Add(viewerConnectionId);
            return Task.CompletedTask;
        }
    }
}
