using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Services;

public class ShutdownServiceWin : IShutdownService
{
    private readonly IDesktopHubConnection _hubConnection;
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly ILogger<ShutdownServiceWin> _logger;

    public ShutdownServiceWin(
        IDesktopHubConnection hubConnection,
        IWindowsUiDispatcher dispatcher,
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
            Application.Exit();
            _dispatcher.InvokeWpf(() =>
            {
                _dispatcher.CurrentApp.Shutdown();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while shutting down.");
        }
    }
}
