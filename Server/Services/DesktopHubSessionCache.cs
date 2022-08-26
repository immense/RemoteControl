using Immense.RemoteControl.Server.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Services
{
    public interface IDesktopHubSessionCache
    {
        ICollection<RemoteControlSession> Sessions { get; }

        void AddOrUpdate(string connectionId, RemoteControlSession session, Func<string, RemoteControlSession, RemoteControlSession> updateFactory);
        bool ContainsKey(string sessionId);

        bool TryGet(string connectionId, out RemoteControlSession session);
        bool TryRemove(string connectionId, out RemoteControlSession session);
    }

    public class DesktopHubSessionCache : IDesktopHubSessionCache
    {
        private static readonly ConcurrentDictionary<string, RemoteControlSession> _sessions = new();

        public ICollection<RemoteControlSession> Sessions => _sessions.Values;

        public void AddOrUpdate(
            string connectionId,
            RemoteControlSession session,
            Func<string, RemoteControlSession, RemoteControlSession> updateFactory)
        {
            _sessions.AddOrUpdate(connectionId, session, updateFactory);
        }

        public bool ContainsKey(string sessionId)
        {
            return _sessions.ContainsKey(sessionId);
        }

        public bool TryGet(string connectionId, out RemoteControlSession session)
        {
            if (_sessions.TryGetValue(connectionId, out var result))
            {
                session = result;
                return true;
            }

            session = RemoteControlSession.Empty;
            return false;
        }
        public bool TryRemove(string connectionId, out RemoteControlSession session)
        {
            if (_sessions.TryRemove(connectionId, out var result))
            {
                session = result;
                return true;
            }

            session = RemoteControlSession.Empty;
            return false;
        }
    }
}
