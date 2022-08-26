using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Abstractions
{
    public interface IHubEventHandler
    {
        void LogRemoteControlStarted(string message, string organizationId);
        Task RestartScreenCaster(string desktopConnectionId, string serviceConnectionId, HashSet<string> viewerList);
        Task NotifyUnattendedSessionReady(string userConnectionId, string desktopConnectionId, string deviceId);
        Task ChangeWindowsSession(string serviceConnectionId, string viewerConnectionId, int targetWindowsSession);
    }
}
