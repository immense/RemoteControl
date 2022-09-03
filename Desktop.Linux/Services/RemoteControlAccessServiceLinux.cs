using Avalonia.Threading;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.ViewModels;
using Immense.RemoteControl.Desktop.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Linux.Services
{
    public class RemoteControlAccessServiceLinux : IRemoteControlAccessService
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IAvaloniaDispatcher _dispatcher;

        public RemoteControlAccessServiceLinux(
            IViewModelFactory viewModelFactory,
            IAvaloniaDispatcher dispatcher)
        {
            _viewModelFactory = viewModelFactory;
            _dispatcher = dispatcher;
        }

        public async Task<bool> PromptForAccess(string requesterName, string organizationName)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
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
}
