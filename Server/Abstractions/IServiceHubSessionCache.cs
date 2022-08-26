using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Abstractions
{
    public interface IServiceHubSessionCache
    {
        /// <summary>
        /// Key is the SignalR connection ID.  Value is the Device ID.
        /// </summary>
        public ConcurrentDictionary<string, string> Sessions { get; }
    }
}
