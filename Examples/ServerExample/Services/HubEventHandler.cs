using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Shared.Enums;
using System.Diagnostics;

namespace ServerExample.Services;

internal class HubEventHandler : IHubEventHandler
{
    private readonly IHttpContextAccessor _contextAccessor;

    public HubEventHandler(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public Task ChangeWindowsSession(RemoteControlSession session, string viewerConnectionId, int targetWindowsSession)
    {
        return Task.CompletedTask;
    }

    public Task InvokeCtrlAltDel(RemoteControlSession session, string viewerConnectionId)
    {
        return Task.CompletedTask;
    }

    public void LogRemoteControlStarted()
    {
    
    }

    public Task NotifySessionChanged(RemoteControlSession sessionInfo, SessionSwitchReasonEx reason, int currentSessionId)
    {
        switch (reason)
        {
            case SessionSwitchReasonEx.ConsoleDisconnect:
            case SessionSwitchReasonEx.RemoteConnect:
            case SessionSwitchReasonEx.RemoteDisconnect:
            case SessionSwitchReasonEx.SessionLogoff:
            case SessionSwitchReasonEx.SessionLock:
            case SessionSwitchReasonEx.SessionRemoteControl:
                // These ones will cause remote control to stop working.  We'll need
                // to launch a new process in the active session or handle this some
                // other way.
                break;
            case SessionSwitchReasonEx.ConsoleConnect:
            case SessionSwitchReasonEx.SessionLogon:
            case SessionSwitchReasonEx.SessionUnlock:
                break;
            default:
                break;
        }

        return Task.CompletedTask;
    }

    public Task NotifyUnattendedSessionReady(RemoteControlSession session, string relativeAccessUrl)
    {
        var request = _contextAccessor.HttpContext?.Request;
        var link = relativeAccessUrl;

        if (request is not null)
        {
            link = $"{request.Scheme}://{request.Host}{relativeAccessUrl}";
        }

        Console.WriteLine("Unattended session ready.  URL:");
        Console.WriteLine(link);

        if (Debugger.IsAttached)
        {
            var psi = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = link
            };
            Process.Start(psi);
        }
        return Task.CompletedTask;
    }

    public Task RestartScreenCaster(RemoteControlSession sessionInfo, HashSet<string> viewerList)
    {
        return Task.CompletedTask;
    }
}
