using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Server.Services;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Server.Hubs;

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
        set
        {
            Context.Items[nameof(SessionInfo)] = value;
        }
    }

    /// <summary>
    /// Used to signal that the desktop process is expected to shut down
    /// and viewers should not be notified.
    /// </summary>
    private bool ShutdownExpected
    {
        get
        {
            return Context.Items.TryGetValue(nameof(ShutdownExpected), out var result) &&
                result is bool typedResult &&
                typedResult;
        }
        set => Context.Items[nameof(ShutdownExpected)] = value;
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

    public async Task<string> GetSessionID()
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
            if (_sessionCache.TryAdd(sessionId, SessionInfo))
            {
                break;
            }
            await Task.Yield();
        }

        SessionInfo.SetSessionReadyState(true);
        return sessionId;
    }

    public Task NotifyRequesterUnattendedReady()
    {
        using var scope = _logger.BeginScope(nameof(NotifyRequesterUnattendedReady));

        if (!_sessionCache.TryGetValue($"{SessionInfo.UnattendedSessionId}", out var session))
        {
            _logger.LogError("Connection not found in cache.");
            return Task.CompletedTask;
        }

        session.SetSessionReadyState(true);
        return Task.CompletedTask;
    }

    public async Task NotifySessionChanged(SessionSwitchReasonEx reason, int currentSessionId)
    {
        await _viewerHub.Clients.Clients(ViewerList).SendAsync("ShowMessage", "Changing sessions");
        ShutdownExpected = true;
        await _hubEvents.NotifySessionChanged(SessionInfo, reason, currentSessionId);
    }

    public Task NotifyViewersRelaunchedScreenCasterReady(string[] viewerIDs)
    {
        SessionInfo.DesktopConnectionId = Context.ConnectionId;
        return _viewerHub.Clients.Clients(viewerIDs).SendAsync("RelaunchedScreenCasterReady", SessionInfo.UnattendedSessionId, SessionInfo.AccessKey);
    }

    public override async Task OnConnectedAsync()
    {
       
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Desktop app disconnected. Shutdown Expected: {expected}.  Viewer Count: {count}", 
            ShutdownExpected, 
            ViewerList.Count);

        if (SessionInfo.Mode == RemoteControlMode.Attended)
        {
            _sessionCache.TryRemove(SessionInfo.AttendedSessionId, out _);
            await _viewerHub.Clients.Clients(ViewerList).SendAsync("ScreenCasterDisconnected");
        }
        else if (SessionInfo.Mode == RemoteControlMode.Unattended && !ShutdownExpected)
        {
            if (ViewerList.Count > 0)
            {
                _logger.LogWarning("Screen caster disconnected and shutdown unexpected.  Reconnecting.");
                await _viewerHub.Clients.Clients(ViewerList).SendAsync("Reconnecting");
                await _hubEvents.RestartScreenCaster(SessionInfo, ViewerList);
            }
            else
            {
                _logger.LogWarning("Screen caster disconnected and shutdown expected.  Removing session.");
                _sessionCache.Remove($"{SessionInfo.UnattendedSessionId}");
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<string> PingViewer(string viewerConnectionId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await _viewerHub.Clients.Client(viewerConnectionId).InvokeAsync<string>("PingViewer", cts.Token);
    }

    public Task<Result> ReceiveUnattendedSessionInfo(Guid unattendedSessionId, string accessKey, string machineName, string requesterName, string organizationName)
    {
        if (_sessionCache.TryGetValue($"{unattendedSessionId}", out var sessionInfo))
        {
            SessionInfo = sessionInfo;
        }

        SessionInfo.Mode = RemoteControlMode.Unattended;
        SessionInfo.DesktopConnectionId = Context.ConnectionId;
        SessionInfo.StartTime = DateTimeOffset.Now;
        SessionInfo.UnattendedSessionId = unattendedSessionId;
        SessionInfo.AccessKey = accessKey;
        SessionInfo.MachineName = machineName;
        SessionInfo.RequesterName = requesterName;
        SessionInfo.OrganizationName = organizationName;

        if (_sessionCache.TryGetValue($"{unattendedSessionId}", out var existingSession) &&
            accessKey != SessionInfo.AccessKey)
        {
            _logger.LogWarning("A desktop session tried to connect, but the access key didn't match.");
            var result = Result.Fail("SessionId already exists on the server.");
            return Task.FromResult(result);
        }

        SessionInfo = _sessionCache.AddOrUpdate($"{unattendedSessionId}", SessionInfo, (k, v) =>
        {
            v.DesktopConnectionId = Context.ConnectionId;
            v.StartTime = DateTimeOffset.Now;
            v.MachineName = machineName;
            v.RequesterName = requesterName;
            v.OrganizationName = organizationName;
            return v;
        });

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


    public async Task SendDesktopStream(IAsyncEnumerable<byte[]> stream, Guid streamId)
    {
        var session = _streamCache.GetOrAdd(streamId, key => new StreamSignaler(streamId));
        session.DesktopConnectionId = Context.ConnectionId;

        try
        {
            session.Stream = stream;
            session.ReadySignal.Release();
            await session.EndSignal.WaitAsync(TimeSpan.FromHours(8));
        }
        finally
        {
            if (_streamCache.TryRemove(session.StreamId, out var signaler))
            {
                signaler.Dispose();
            }
        }
    }
}
