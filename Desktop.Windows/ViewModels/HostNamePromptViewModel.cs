using CommunityToolkit.Mvvm.ComponentModel;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Windows.Services;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels
{
    public partial class HostNamePromptViewModel : BrandedViewModelBase
    {
        [ObservableProperty]
        private string _host = "https://";

        public HostNamePromptViewModel(
            IBrandingProvider brandingProvider,
            IWpfDispatcher wpfDispatcher,
            ILogger<BrandedViewModelBase> logger)
            : base(brandingProvider, wpfDispatcher, logger)
        {
        }
    }
}
