using Immense.RemoteControl.Server.Abstractions;

namespace ServerExample.Services
{
    internal class HubEventHandler : IHubEventHandler
    {
        public Task ChangeWindowsSession(string serviceConnectionId, string viewerConnectionId, int targetWindowsSession)
        {
            return Task.CompletedTask;
        }

        public void LogRemoteControlStarted(string message, string organizationId)
        {
        
        }

        public Task NotifyUnattendedSessionReady(string userConnectionId, string desktopConnectionId, string deviceId)
        {
            return Task.CompletedTask;
        }

        public Task RestartScreenCaster(string desktopConnectionId, string serviceConnectionId, HashSet<string> viewerList)
        {
            return Task.CompletedTask;
        }
    }
}
