using Immense.RemoteControl.Server.Abstractions;
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

    RemoteControlSession GetOrAdd(string sessionId, Func<string, RemoteControlSession> valueFactory);
    Task RemoveExpiredSessions();
    bool TryAdd(string sessionId, RemoteControlSession session);

    bool TryGetValue(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session);
    bool TryRemove(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session);
}

internal class DesktopHubSessionCache : IDesktopHubSessionCache
{
    private static readonly ConcurrentDictionary<string, RemoteControlSession> _sessions = new();
    private readonly IHubEventHandler _hubEventHandler;
    private readonly ILogger<DesktopHubSessionCache> _logger;
    private readonly ISystemTime _systemTime;
    public DesktopHubSessionCache(
        ISystemTime systemTime,
        IHubEventHandler hubEventHandler,
        ILogger<DesktopHubSessionCache> logger)
    {
        _systemTime = systemTime;
        _hubEventHandler = hubEventHandler;
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
        var added = true;

        var resultSession = _sessions.AddOrUpdate(sessionId, session, (k,v) =>
        {
            // If we get into the update factory, then we're not adding a new one.
            added = false;
            return updateFactory(k, v);
        });

        if (added)
        {
            NotifySessionAdded(resultSession);
        }

        return resultSession;
    }

    public RemoteControlSession GetOrAdd(string sessionId, Func<string, RemoteControlSession> valueFactory)
    {
        return _sessions.GetOrAdd(sessionId, (key) =>
        {
            var session = valueFactory(key);
            NotifySessionAdded(session);
            return session;
        });
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
                    NotifySessionRemoved(expiredSession);
                    expiredSession.Dispose();
                }
            }
        }
        return Task.CompletedTask;
    }

    public bool TryAdd(string sessionId, RemoteControlSession session)
    {
        if (_sessions.TryAdd(sessionId, session))
        {
            NotifySessionAdded(session);
            return true;
        }

        return false;
    }

    public bool TryGetValue(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public bool TryRemove(string sessionId, [NotNullWhen(true)] out RemoteControlSession? session)
    {
        if (_sessions.TryRemove(sessionId, out session))
        {
            try
            {
                NotifySessionRemoved(session);
                session.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RemoteControlSession ID {id}.", sessionId);
            }

            return true;
        }
        return false;
    }

    private void NotifySessionAdded(RemoteControlSession session)
    {
        try
        {
            _ = _hubEventHandler.NotifyDesktopSessionAdded(session);
        }
        catch { } // Ignore errors thrown by consumer.
    }
    private void NotifySessionRemoved(RemoteControlSession session)
    {
        try
        {
            _ = _hubEventHandler.NotifyDesktopSessionRemoved(session);
        }
        catch { } // Ignore errors thrown by consumer.
    }
}
