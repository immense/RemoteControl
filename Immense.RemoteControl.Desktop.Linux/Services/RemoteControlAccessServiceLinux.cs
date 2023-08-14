using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.Views;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class RemoteControlAccessServiceLinux : IRemoteControlAccessService
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IAvaloniaDispatcher _dispatcher;
    private readonly ILogger<RemoteControlAccessServiceLinux> _logger;
    private volatile int _promptCount = 0;

    public RemoteControlAccessServiceLinux(
        IViewModelFactory viewModelFactory,
        IAvaloniaDispatcher dispatcher,
        ILogger<RemoteControlAccessServiceLinux> logger)
    {
        _viewModelFactory = viewModelFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public bool IsPromptOpen => _promptCount > 0;

    public async Task<bool> PromptForAccess(string requesterName, string organizationName)
    {
        return await _dispatcher.InvokeAsync(async () =>
        {
            try
            {
                Interlocked.Increment(ref _promptCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while prompting for remote control access.");
            }
            finally
            {
                Interlocked.Decrement(ref _promptCount);
            }
            var viewModel = _viewModelFactory.CreatePromptForAccessViewModel(requesterName, organizationName);
            var promptWindow = new PromptForAccessWindow()
            {
                DataContext = viewModel
            };

            var closeSignal = new SemaphoreSlim(0, 1);
            promptWindow.Closed += (sender, arg) =>
            {
                closeSignal.Release();
            };

            // We can't use ShowDialog here because the MainWindow might not exist,
            // which is required.
            promptWindow.Show();

            var result = await closeSignal.WaitAsync(TimeSpan.FromMinutes(1));

            if (!result)
            {
                promptWindow.Close();
                return false;
            }

            return viewModel.PromptResult;
        });
    }
}
