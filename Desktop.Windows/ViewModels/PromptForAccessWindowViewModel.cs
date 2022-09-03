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
    public interface IPromptForAccessWindowViewModel : IBrandedViewModelBase
    {
        string OrganizationName { get; set; }
        bool PromptResult { get; set; }
        string RequesterName { get; set; }
        RelayCommand<Window> SetResultNoCommand { get; }
        RelayCommand<Window> SetResultYesCommand { get; }

        void SetResultNo(Window? promptWindow);
        void SetResultYes(Window? promptWindow);
    }

    public class PromptForAccessWindowViewModel : BrandedViewModelBase, IPromptForAccessWindowViewModel
    {
        public PromptForAccessWindowViewModel(
            string requesterName,
            string organizationName,
            IBrandingProvider brandingProvider,
            IWindowsUiDispatcher dispatcher,
            ILogger<PromptForAccessWindowViewModel> logger)
            : base(brandingProvider, dispatcher, logger)
        {
            if (!string.IsNullOrWhiteSpace(requesterName))
            {
                RequesterName = requesterName;
            }

            if (!string.IsNullOrWhiteSpace(requesterName))
            {
                OrganizationName = organizationName;
            }

            SetResultNoCommand = new RelayCommand<Window>(SetResultNo);
            SetResultYesCommand = new RelayCommand<Window>(SetResultYes);
        }

        public string OrganizationName
        {
            get => Get<string>() ?? "your IT provider";
            set => Set(value);
        }

        public bool PromptResult { get; set; }
        public string RequesterName
        {
            get => Get<string>() ?? "a technician";
            set => Set(value);
        }

        public RelayCommand<Window> SetResultNoCommand { get; }
        public RelayCommand<Window> SetResultYesCommand { get; }


        public void SetResultNo(Window? promptWindow)
        {
            if (promptWindow is null)
            {
                return;
            }

            PromptResult = false;
            promptWindow.Close();
        }

        public void SetResultYes(Window? promptWindow)
        {
            if (promptWindow is null)
            {
                return;
            }

            PromptResult = true;
            promptWindow.Close();
        }
    }
}
