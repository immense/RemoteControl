using CommunityToolkit.Mvvm.ComponentModel;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Windows.Services;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels
{
    public class HostNamePromptViewModel : BrandedViewModelBase
    {
        public HostNamePromptViewModel(
            IBrandingProvider brandingProvider,
            IWpfDispatcher wpfDispatcher,
            ILogger<BrandedViewModelBase> logger)
            : base(brandingProvider, wpfDispatcher, logger)
        {
        }

        public string Host
        {
            get => Get<string>() ?? "https://";
            set => Set(value);
        }
    }
}
