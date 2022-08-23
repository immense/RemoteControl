using Immense.RemoteControl.AspNetCore.Models;
using Immense.RemoteControl.AspNetCore.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Immense.RemoteControl.AspNetCore.Hubs
{
    public interface IDesktopClientHub
    {
     
    }

    public class DesktopClientHub : Hub, IDesktopClientHub
    {
        private readonly IDesktopHubSessionCache _sessionCache;
        private readonly IHubContext<ViewerHub> _viewerHub;
        private readonly ILogger<DesktopClientHub> _logger;

        public DesktopClientHub(
            IDesktopHubSessionCache sessionCache,
            IHubContext<ViewerHub> viewerHubContext,
            ILogger<DesktopClientHub> logger)
        {
            _sessionCache = sessionCache;
            _viewerHub = viewerHubContext;
            _logger = logger;
        }

      

        private RemoteControlSession SessionInfo
        {
            get
            {
                if (Context.Items.TryGetValue("SessionInfo", out var result) &&
                    result is RemoteControlSession session)
                {
                    return session;
                }

                var newSession = new RemoteControlSession();
                Context.Items["SessionInfo"] = newSession;
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

            while (string.IsNullOrWhiteSpace(sessionId) || _sessionCache.ContainsKey(sessionId))
            {
                for (var i = 0; i < 3; i++)
                {
                    sessionId += random.Next(0, 999).ToString().PadLeft(3, '0');
                }
            }

            Context.Items["SessionID"] = sessionId;

            if (!_sessionCache.TryGet(Context.ConnectionId, out var session))
            {
                _logger.LogError("Connection not found in cache.");
                return string.Empty;
            }

            session.AttendedSessionID = sessionId;
            return sessionId;
        }

        public Task NotifyRequesterUnattendedReady(string browserHubConnectionID)
        {
            using var scope = _logger.BeginScope(nameof(NotifyRequesterUnattendedReady));

            if (!_sessionCache.TryGet(Context.ConnectionId, out var session))
            {
                _logger.LogError("Connection not found in cache.");
                return Task.CompletedTask;
            }

            var deviceId = session.DeviceID;
            _circuitManager.InvokeOnConnection(browserHubConnectionID, CircuitEventName.UnattendedSessionReady, Context.ConnectionId, deviceId);
            return Task.CompletedTask;
        }

        public Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs)
        {
            return _viewerHub.Clients.Clients(viewerIDs).SendAsync("RelaunchedScreenCasterReady", Context.ConnectionId);
        }

        public override async Task OnConnectedAsync()
        {
            SessionInfo.CasterSocketID = Context.ConnectionId;
            SessionInfo.StartTime = DateTimeOffset.Now;
            _sessionCache.AddOrUpdate(Context.ConnectionId, SessionInfo, (id, si) => SessionInfo);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _sessionCache.TryRemove(Context.ConnectionId, out _);

            if (SessionInfo.Mode == RemoteControlMode.Attended)
            {
                await _viewerHub.Clients.Clients(ViewerList).SendAsync("ScreenCasterDisconnected");
            }
            else if (SessionInfo.Mode == RemoteControlMode.Unattended)
            {
                if (ViewerList.Count > 0)
                {
                    await _viewerHub.Clients.Clients(ViewerList).SendAsync("Reconnecting");
                    await _agentHubContext.Clients.Client(SessionInfo.ServiceID).SendAsync("RestartScreenCaster", ViewerList, SessionInfo.ServiceID, Context.ConnectionId);
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

        public Task SendMessageToViewer(string viewerId, string message)
        {
            return _viewerHub.Clients.Client(viewerId).SendAsync("ShowMessage", message);
        }

        public Task SendCtrlAltDelToAgent()
        {
            return _viewerHub.Clients.Client(SessionInfo.ServiceID).SendAsync("CtrlAltDel");
        }

        public Task SendDtoToBrowser(byte[] dto, string viewerId)
        {
            return _viewerHub.Clients.Client(viewerId).SendAsync("SendDtoToBrowser", dto);
        }

        public Task ViewerConnected(string viewerConnectionId)
        {
            ViewerList.Add(viewerConnectionId);
            return Task.CompletedTask;
        }
    }
}
