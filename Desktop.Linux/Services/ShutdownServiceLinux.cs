using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Linux.Services
{
    public class ShutdownServiceLinux : IShutdownService
    {
        private readonly IDesktopHubConnection _hubConnection;
        private readonly IAvaloniaDispatcher _dispatcher;
        private readonly ILogger<ShutdownServiceLinux> _logger;

        public ShutdownServiceLinux(
            IDesktopHubConnection hubConnection,
            IAvaloniaDispatcher dispatcher,
            ILogger<ShutdownServiceLinux> logger)
        {
            _hubConnection = hubConnection;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Shutdown()
        {
            _logger.LogDebug("Exiting process ID {processId}.", Environment.ProcessId);
            await _hubConnection.DisconnectAllViewers();
            _dispatcher.Shutdown();
        }
    }
}
