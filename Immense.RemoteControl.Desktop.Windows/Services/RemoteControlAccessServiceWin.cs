using Immense.RemoteControl.Desktop.UI.WPF.Views;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Services;

public class RemoteControlAccessServiceWin : IRemoteControlAccessService
{
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILogger<RemoteControlAccessServiceWin> _logger;
    private volatile int _promptCount = 0;

    public RemoteControlAccessServiceWin(
        IWindowsUiDispatcher dispatcher,
        IViewModelFactory viewModelFactory,
        ILogger<RemoteControlAccessServiceWin> logger)
    {
        _dispatcher = dispatcher;
        _viewModelFactory = viewModelFactory;
        _logger = logger;
    }

    public bool IsPromptOpen => _promptCount > 0;

    public Task<bool> PromptForAccess(string requesterName, string organizationName)
    {
        var result = _dispatcher.InvokeWpf(() =>
        {
            try
            {
                Interlocked.Increment(ref _promptCount);

                var viewModel = _viewModelFactory.CreatePromptForAccessViewModel(requesterName, organizationName);
                var promptWindow = new PromptForAccessWindow(viewModel);
                promptWindow.ShowDialog();

                return viewModel.PromptResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while prompting for remote control access.");
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref _promptCount);
            }
        });

        return Task.FromResult(result);
    }
}
