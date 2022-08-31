using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Windows.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels
{
    public partial class PromptForAccessWindowViewModel : BrandedViewModelBase
    {
        [ObservableProperty]
        private string _organizationName = "your IT provider";

        [ObservableProperty]
        private string _requesterName = "a technician";

#nullable disable
        [Obsolete("Parameterless constructor used only for WPF design-time DataContext")]
        public PromptForAccessWindowViewModel() { }
#nullable enable


        public PromptForAccessWindowViewModel(
            string requesterName, 
            string organizationName,
            IBrandingProvider brandingProvider,
            IWpfDispatcher wpfDispatcher,
            ILogger<PromptForAccessWindowViewModel> logger)
            : base(brandingProvider, wpfDispatcher, logger)
        {
            if (!string.IsNullOrWhiteSpace(requesterName))
            {
                RequesterName = requesterName;
            }

            if (!string.IsNullOrWhiteSpace(requesterName))
            {
                OrganizationName = organizationName;
            }
        }


        public bool PromptResult { get; set; }

        [RelayCommand]
        public void SetResultNo(Window promptWindow)
        {
            PromptResult = false;
            promptWindow.Close();
        }

        [RelayCommand]
        public void SetResultYes(Window promptWindow)
        {
            PromptResult = true;
            promptWindow.Close();
        }
    }
}
