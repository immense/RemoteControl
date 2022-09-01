using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Server.Services;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Models.Dtos;
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
        private readonly IDesktopStreamCache _streamCache;
        private readonly IHubContext<ViewerHub> _viewerHub;
        public DesktopHub(
            IDesktopHubSessionCache sessionCache,
            IDesktopStreamCache streamCache,
            IHubContext<ViewerHub> viewerHubContext,
            IHubEventHandler hubEvents,
            ILogger<DesktopHub> logger)
        {
            _sessionCache = sessionCache;
            _streamCache = streamCache;
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

            SessionInfo.Mode = RemoteControlMode.Attended;

            var random = new Random();
            var sessionId = string.Empty;

            while (true)
            {
                sessionId = "";
                for (var i = 0; i < 3; i++)
                {
                    sessionId += random.Next(0, 999).ToString().PadLeft(3, '0');
                }

                SessionInfo.AttendedSessionId = sessionId;
                if (_sessionCache.Sessions.TryAdd(sessionId, SessionInfo))
                {
                    break;
                }

            }

            return sessionId;
        }

        public async Task NotifyRequesterUnattendedReady()
        {
            using var scope = _logger.BeginScope(nameof(NotifyRequesterUnattendedReady));

            if (!_sessionCache.Sessions.TryGetValue(SessionInfo.UnattendedSessionId, out var session))
            {
                _logger.LogError("Connection not found in cache.");
                return;
            }

            var accessLink = $"/RemoteControl/Viewer?mode=Unattended&sessionId={session.UnattendedSessionId}&accessKey={session.AccessKey}&viewonly=False";
            await _hubEvents.NotifyUnattendedSessionReady(session, accessLink);
        }

        public Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs)
        {
            return _viewerHub.Clients.Clients(viewerIDs).SendAsync("RelaunchedScreenCasterReady", SessionInfo.UnattendedSessionId, SessionInfo.AccessKey);
        }

        public override async Task OnConnectedAsync()
        {
           
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
                    await _hubEvents.RestartScreenCaster(SessionInfo, ViewerList);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<Result> ReceiveUnattendedSessionInfo(string unattendedSessionId, string accessKey, string machineName, string requesterName, string organizationName)
        {
            SessionInfo.Mode = RemoteControlMode.Unattended;
            SessionInfo.DesktopConnectionId = Context.ConnectionId;
            SessionInfo.StartTime = DateTimeOffset.Now;
            SessionInfo.UnattendedSessionId = unattendedSessionId;
            SessionInfo.AccessKey = accessKey;
            SessionInfo.MachineName = machineName;
            SessionInfo.RequesterName = requesterName;
            SessionInfo.OrganizationName = organizationName;

            if (!_sessionCache.Sessions.TryAdd(unattendedSessionId, SessionInfo))
            {
                var result = Result.Fail("SessionId already exists on the server.");
                return Task.FromResult(result);
            }

            return Task.FromResult(Result.Ok());
        }

        public Task ReceiveAttendedSessionInfo(string machineName)
        {
            SessionInfo.DesktopConnectionId = Context.ConnectionId;
            SessionInfo.StartTime = DateTimeOffset.Now;
            SessionInfo.MachineName = machineName;

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

        public Task SendStreamReady(string viewerConnectionId)
        {
            return _viewerHub.Clients.Client(viewerConnectionId).SendAsync("SendStreamReady");
        }
        public async Task SendDesktopStream(IAsyncEnumerable<byte[]> stream, Guid streamId)
        {
            var session = _streamCache.GetOrAdd(streamId, key => new StreamSignaler(streamId));

            try
            {
                session.Stream = stream;
                session.ReadySignal.Release();
                await session.EndSignal.WaitAsync(TimeSpan.FromHours(8));
            }
            finally
            {
                _streamCache.TryRemove(session.StreamId, out _);
            }
        }
        public Task ViewerConnected(string viewerConnectionId)
        {
            ViewerList.Add(viewerConnectionId);
            return Task.CompletedTask;
        }
    }
}
