using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Filters;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Server.Services;
using Immense.RemoteControl.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Server.Hubs;

[ServiceFilter(typeof(ViewerAuthorizationFilter))]
public class ViewerHub : Hub
{
    private readonly IHubContext<DesktopHub> _desktopHub;
    private readonly IDesktopHubSessionCache _desktopSessionCache;
    private readonly IHubEventHandler _hubEvents;
    private readonly ILogger<ViewerHub> _logger;
    private readonly IDesktopStreamCache _streamCache;

    public ViewerHub(
        IHubEventHandler hubEvents,
        IDesktopHubSessionCache desktopSessionCache,
        IDesktopStreamCache streamCache,
        IHubContext<DesktopHub> desktopHub,
        ILogger<ViewerHub> logger)
    {
        _hubEvents = hubEvents;
        _desktopSessionCache = desktopSessionCache;
        _streamCache = streamCache;
        _desktopHub = desktopHub;
        _logger = logger;
    }

    private string RequesterDisplayName
    {
        get
        {
            if (Context.Items.TryGetValue(nameof(RequesterDisplayName), out var result) &&
                result is string requesterName)
            {
                return requesterName;
            }
            return string.Empty;
        }
        set
        {
            Context.Items[nameof(RequesterDisplayName)] = value;
        }
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
    public async Task<Result> ChangeWindowsSession(int targetWindowsSession)
    {
        if (SessionInfo.Mode != RemoteControlMode.Unattended)
        {
            return Result.Fail("Only available in unattended mode.");
        }

        SessionInfo.ViewerList.Remove(Context.ConnectionId);
        await _desktopHub.Clients.Client(SessionInfo.DesktopConnectionId).SendAsync("ViewerDisconnected", Context.ConnectionId);

        SessionInfo = SessionInfo.CreateNew();
        _desktopSessionCache.AddOrUpdate($"{SessionInfo.UnattendedSessionId}", SessionInfo);

        await _hubEvents.ChangeWindowsSession(SessionInfo, Context.ConnectionId, targetWindowsSession);
        return Result.Ok();
    }

    public async IAsyncEnumerable<byte[]> GetDesktopStream()
    {
        var result = await _streamCache.WaitForStreamSession(SessionInfo.StreamId, TimeSpan.FromSeconds(30));

        if (!result.IsSuccess)
        {
            _logger.LogError("Timed out while waiting for desktop stream.");
            await Clients.Caller.SendAsync("ShowMessage", "Request timed out");
            yield break;
        }

        var signaler = result.Value;

        if (signaler?.Stream is null)
        {
            _logger.LogError("Stream was null.");
            yield break;
        }

        try
        {
            await foreach (var chunk in signaler.Stream)
            {
                yield return chunk;
            }
        }
        finally
        {
            signaler.EndSignal.Release();
            _logger.LogInformation("Streaming session ended for {sessionId}.", SessionInfo.StreamId);
        }
    }

    public Task InvokeCtrlAltDel()
    {
        return _hubEvents.InvokeCtrlAltDel(SessionInfo, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!string.IsNullOrWhiteSpace(SessionInfo.DesktopConnectionId))
        {
            await _desktopHub.Clients.Client(SessionInfo.DesktopConnectionId).SendAsync("ViewerDisconnected", Context.ConnectionId);
        }

        SessionInfo.ViewerList.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public Task SendDtoToClient(byte[] dtoWrapper)
    {
        if (string.IsNullOrWhiteSpace(SessionInfo.DesktopConnectionId))
        {
            return Task.CompletedTask;
        }

        return _desktopHub.Clients.Client(SessionInfo.DesktopConnectionId).SendAsync("SendDtoToClient", dtoWrapper, Context.ConnectionId);
    }
    public async Task SendScreenCastRequestToDevice(string sessionId, string accessKey, string requesterName)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        if (!_desktopSessionCache.TryGetValue(sessionId, out var session))
        {
            await Clients.Caller.SendAsync("SessionIDNotFound");
            return;
        }

        if (session.Mode == RemoteControlMode.Unattended &&
            accessKey != session.AccessKey)
        {
            _logger.LogError("Access key does not match for unattended session.  " +
                "Session ID: {sessionId}.  " +
                "Requester Name: {requesterName}.  " +
                "Requester Connection ID: {connectionId}",
                sessionId,
                requesterName,
                Context.ConnectionId);
            await Clients.Caller.SendAsync("Unauthorized");
            return;
        }

        SessionInfo = session;
        SessionInfo.ViewerList.Add(Context.ConnectionId);
        SessionInfo.StreamId = Guid.NewGuid();
        RequesterDisplayName = requesterName;

        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            SessionInfo.RequesterUserName = Context.User.Identity.Name ?? string.Empty;
        }

        var logMessage = $"Remote control session requested.  " +
                            $"Login ID (if logged in): {Context.User?.Identity?.Name}.  " +
                            $"Machine Name: {SessionInfo.MachineName}.  " +
                            $"Requester Name (if specified): {RequesterDisplayName}.  " +
                            $"Connection ID: {Context.ConnectionId}. User ID: {Context.UserIdentifier}.  " +
                            $"Screen Caster Connection ID: {SessionInfo.DesktopConnectionId}.  " +
                            $"Mode: {SessionInfo.Mode}.  " +
                            $"Requester IP Address: {Context.GetHttpContext()?.Connection?.RemoteIpAddress}";

        _logger.LogInformation("{msg}", logMessage);

        if (SessionInfo.Mode == RemoteControlMode.Unattended)
        {
            await _desktopHub.Clients.Client(SessionInfo.DesktopConnectionId).SendAsync(
                "GetScreenCast",
                Context.ConnectionId,
                RequesterDisplayName,
                SessionInfo.NotifyUserOnStart,
                SessionInfo.RequireConsent,
                SessionInfo.OrganizationName,
                SessionInfo.StreamId);
        }
        else
        {
            SessionInfo.Mode = RemoteControlMode.Attended;
            await Clients.Caller.SendAsync("RequestingScreenCast");
            await _desktopHub.Clients.Client(SessionInfo.DesktopConnectionId).SendAsync(
                "RequestScreenCast", 
                Context.ConnectionId, 
                RequesterDisplayName,
                SessionInfo.NotifyUserOnStart,
                SessionInfo.StreamId);
        }
    }

}
