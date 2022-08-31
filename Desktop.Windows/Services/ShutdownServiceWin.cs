using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public class ShutdownServiceWin : IShutdownService
    {
        private readonly IDesktopHubConnection _hubConnection;
        private readonly IWpfDispatcher _dispatcher;
        private readonly ILogger<ShutdownServiceWin> _logger;

        public ShutdownServiceWin(
            IDesktopHubConnection hubConnection,
            IWpfDispatcher dispatcher,
            ILogger<ShutdownServiceWin> logger)
        {
            _hubConnection = hubConnection;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Shutdown()
        {
            try
            {
                _logger.LogInformation("Exiting process ID {procId}.", Environment.ProcessId);
                await _hubConnection.DisconnectAllViewers();
                await _hubConnection.Disconnect();
                System.Windows.Forms.Application.Exit();
                _dispatcher.Invoke(() =>
                {
                    WpfApp.Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while shutting down.");
            }
        }
    }
}
