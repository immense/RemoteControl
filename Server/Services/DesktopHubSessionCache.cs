using Immense.RemoteControl.Server.Models;
using System.Collections.Concurrent;

namespace Immense.RemoteControl.Server.Services;

public interface IDesktopHubSessionCache
{
    ConcurrentDictionary<string, RemoteControlSession> Sessions { get; }
}

internal class DesktopHubSessionCache : IDesktopHubSessionCache
{
    private static readonly ConcurrentDictionary<string, RemoteControlSession> _sessions = new();

    /// <summary>
    /// Contains the active remote control sessions.  The key is the --session-id param that
    /// was originally passed int the desktop process.
    /// </summary>
    public ConcurrentDictionary<string, RemoteControlSession> Sessions => _sessions;
}
