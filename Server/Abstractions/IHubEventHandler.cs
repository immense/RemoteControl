using Immense.RemoteControl.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Abstractions
{
    public interface IHubEventHandler
    {
        Task RestartScreenCaster(RemoteControlSession sessionInfo, HashSet<string> viewerList);
        Task NotifyUnattendedSessionReady(RemoteControlSession session, string relativeAccessUrl);
        Task ChangeWindowsSession(RemoteControlSession session, string viewerConnectionId, int targetWindowsSession);
    }
}
