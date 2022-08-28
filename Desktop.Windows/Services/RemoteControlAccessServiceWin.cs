using Immense.RemoteControl.Desktop.Windows.ViewModels;
using Remotely.Desktop.Core.Interfaces;
using Remotely.Desktop.Win.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public class RemoteControlAccessServiceWin : IRemoteControlAccessService
    {
        public Task<bool> PromptForAccess(string requesterName, string organizationName)
        {
            var result = App.Current.Dispatcher.Invoke(() =>
            {
                var promptWindow = new PromptForAccessWindow();
                var viewModel = promptWindow.DataContext as PromptForAccessWindowViewModel;
                if (!string.IsNullOrWhiteSpace(requesterName))
                {
                    viewModel.RequesterName = requesterName;
                }
                if (!string.IsNullOrWhiteSpace(organizationName))
                {
                    viewModel.OrganizationName = organizationName;
                }
                promptWindow.ShowDialog();

                return viewModel.PromptResult;
            });

            return Task.FromResult(result);
        }
    }
}
