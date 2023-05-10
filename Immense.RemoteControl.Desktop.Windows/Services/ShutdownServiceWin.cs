using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Windows.Services;

public class ShutdownServiceWin : IShutdownService
{
    private readonly IDesktopHubConnection _hubConnection;
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IAppState _appState;
    private readonly ILogger<ShutdownServiceWin> _logger;

    public ShutdownServiceWin(
        IDesktopHubConnection hubConnection,
        IWindowsUiDispatcher dispatcher,
        IAppState appState,
        ILogger<ShutdownServiceWin> logger)
    {
        _hubConnection = hubConnection;
        _dispatcher = dispatcher;
        _appState = appState;
        _logger = logger;
    }

    public async Task Shutdown()
    {
        try
        {
            _logger.LogInformation("Exiting process ID {procId}.", Environment.ProcessId);
            await TryDisconnectViewers();
            Application.Exit();
            try
            {
                _dispatcher.InvokeWpf(_dispatcher.CurrentApp.Shutdown);
            }
            // This is expected to happen sometimes.
            catch (TaskCanceledException)
            {
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while shutting down.");
            Environment.Exit(1);
        }
    }

    private async Task TryDisconnectViewers()
    {
        try
        {
            if (_hubConnection.IsConnected && _appState.Viewers.Any())
            {
                await _hubConnection.DisconnectAllViewers();
                await _hubConnection.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending shutdown notice to viewers.");
        }
    }
}
