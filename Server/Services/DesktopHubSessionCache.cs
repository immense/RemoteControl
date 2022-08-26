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
        ConcurrentDictionary<string, RemoteControlSession> Sessions { get; }
    }

    public class DesktopHubSessionCache : IDesktopHubSessionCache
    {
        private static readonly ConcurrentDictionary<string, RemoteControlSession> _sessions = new();

        public ConcurrentDictionary<string, RemoteControlSession> Sessions => _sessions;
    }
}
