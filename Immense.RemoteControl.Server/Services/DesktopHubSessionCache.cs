using Immense.RemoteControl.Server.Models;
using System.Collections.Concurrent;

namespace Immense.RemoteControl.Server.Services;

public interface IDesktopHubSessionCache
{
    /// <summary>
    /// Contains the active remote control sessions.  The key is the --session-id param that
    /// was originally passed int the desktop process.
    /// </summary>
    ConcurrentDictionary<string, RemoteControlSession> Sessions { get; }
}

internal class DesktopHubSessionCache : IDesktopHubSessionCache
{
    private static readonly ConcurrentDictionary<string, RemoteControlSession> _sessions = new();

    /// <inheritdoc/>
    public ConcurrentDictionary<string, RemoteControlSession> Sessions => _sessions;
}
