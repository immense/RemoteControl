using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using System.Diagnostics;

namespace ServerExample.Services
{
    internal class HubEventHandler : IHubEventHandler
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public HubEventHandler(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Task ChangeWindowsSession(RemoteControlSession session, string viewerConnectionId, int targetWindowsSession)
        {
            return Task.CompletedTask;
        }

        public void LogRemoteControlStarted(string message, string organizationId)
        {
        
        }


        public Task NotifyUnattendedSessionReady(RemoteControlSession session, string relativeAccessUrl)
        {
            var request = _contextAccessor.HttpContext?.Request;
            var link = relativeAccessUrl;

            if (request is not null)
            {
                link = $"{request.Scheme}://{request.Host}{relativeAccessUrl}";
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

        public Task RestartScreenCaster(RemoteControlSession sessionInfo, HashSet<string> viewerList)
        {
            return Task.CompletedTask;
        }
    }
}
