using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Immense.RemoteControl.Server.Services;

public interface IDesktopHubSessionCache
{
    IEnumerable<RemoteControlSession> Sessions { get; }
    RemoteControlSession AddOrUpdate(string sessionId, RemoteControlSession session);
    RemoteControlSession AddOrUpdate(
        string sessionId,
        RemoteControlSession session,
        Func<string, RemoteControlSession, RemoteControlSession> updateFactory);
    void Remove(string sessionId);
    Task RemoveExpiredSessions();
    bool TryAdd(string sessionId, RemoteControlSession session);

    bool TryGetValue(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session);
    bool TryRemove(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session);
}

internal class DesktopHubSessionCache : IDesktopHubSessionCache
{
    private static readonly ConcurrentDictionary<string, RemoteControlSession> _sessions = new();
    private readonly ILogger<DesktopHubSessionCache> _logger;
    private readonly ISystemTime _systemTime;

    public DesktopHubSessionCache(
        ISystemTime systemTime,
        ILogger<DesktopHubSessionCache> logger)
    {
        _systemTime = systemTime;
        _logger = logger;
    }

    public IEnumerable<RemoteControlSession> Sessions => _sessions.Values;
    public RemoteControlSession AddOrUpdate(string sessionId, RemoteControlSession session)
    {
        return AddOrUpdate(sessionId, session, (k, v) =>
        {
            v.Dispose();
            return session;
        });
    }

    public RemoteControlSession AddOrUpdate(
        string sessionId, 
        RemoteControlSession session, 
        Func<string, RemoteControlSession, RemoteControlSession> updateFactory)
    {
        return _sessions.AddOrUpdate(sessionId, session, updateFactory);
    }

    public void Remove(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
        }
    }

    public Task RemoveExpiredSessions()
    {
        foreach (var session in _sessions)
        {
            if (session.Value.Mode is RemoteControlMode.Unattended or RemoteControlMode.Unknown &&
                !session.Value.ViewerList.Any() &&
                session.Value.Created < _systemTime.Now.AddMinutes(-1))
            {
                _logger.LogWarning("Removing expired session: {session}", JsonSerializer.Serialize(session.Value));
                if (_sessions.TryRemove(session.Key, out var expiredSession))
                {
                    expiredSession.Dispose();
                }
            }
        }
        return Task.CompletedTask;
    }

    public bool TryAdd(string sessionId, RemoteControlSession session)
    {
        return _sessions.TryAdd(sessionId, session);
    }

    public bool TryGetValue(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public bool TryRemove(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session)
    {
        if (_sessions.TryRemove(sessionId, out session))
        {
            session.Dispose();
            return true;
        }
        return false;
    }
}
