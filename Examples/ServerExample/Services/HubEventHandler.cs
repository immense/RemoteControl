using Immense.RemoteControl.Server.Abstractions;
using System.Diagnostics;

namespace ServerExample.Services
{
    internal class HubEventHandler : IHubEventHandler
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IServiceHubSessionCache _serviceSessionCache;

        public HubEventHandler(
            IHttpContextAccessor contextAccessor,
            IServiceHubSessionCache serviceSessionCache)
        {
            _contextAccessor = contextAccessor;
            _serviceSessionCache = serviceSessionCache;
        }

        public Task ChangeWindowsSession(string serviceConnectionId, string viewerConnectionId, int targetWindowsSession)
        {
            return Task.CompletedTask;
        }

        public void LogRemoteControlStarted(string message, string organizationId)
        {
        
        }

        public Task NotifyUnattendedSessionReady(string userConnectionId, string desktopConnectionId, string deviceId)
        {
            var link = $"/RemoteControl/Viewer?casterID={desktopConnectionId}&viewonly=False";

            var request = _contextAccessor.HttpContext?.Request;

            if (request is not null)
            {
                link = $"{request.Scheme}://{request.Host}{link}";
            }

            Console.WriteLine("Unattended session ready.  URL:");
            Console.WriteLine(link);

            if (Debugger.IsAttached)
            {
                var psi = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = link
                };
                Process.Start(psi);
            }
            return Task.CompletedTask;
        }

        public Task RestartScreenCaster(string desktopConnectionId, string serviceConnectionId, HashSet<string> viewerList)
        {
            return Task.CompletedTask;
        }
    }
}
