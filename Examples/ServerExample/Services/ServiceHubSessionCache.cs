using Immense.RemoteControl.Server.Abstractions;
using System.Collections.Concurrent;

namespace ServerExample.Services
{
    internal class ServiceHubSessionCache : IServiceHubSessionCache
    {
        public ConcurrentDictionary<string, string> Sessions { get; } = new();
    }
}