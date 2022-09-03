using CommunityToolkit.Mvvm.ComponentModel;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels
{
    public interface IHostNamePromptViewModel
    {
        string Host { get; set; }
    }

    public class HostNamePromptViewModel : BrandedViewModelBase, IHostNamePromptViewModel
    {
        public HostNamePromptViewModel(
            IBrandingProvider brandingProvider,
            IWindowsUiDispatcher dispatcher,
            ILogger<BrandedViewModelBase> logger)
            : base(brandingProvider, dispatcher, logger)
        {
        }

        public string Host
        {
            get => Get<string>() ?? "https://";
            set => Set(value);
        }
    }
}
