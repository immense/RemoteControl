using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.UI.ViewModels;

public interface ISessionIndicatorWindowViewModel
{

}
public class SessionIndicatorWindowViewModel : BrandedViewModelBase, ISessionIndicatorWindowViewModel
{
    public SessionIndicatorWindowViewModel(
        IBrandingProvider brandingProvider,
        IAvaloniaDispatcher dispatcher,
        ILogger<BrandedViewModelBase> logger)
        : base(brandingProvider, dispatcher, logger)
    {
    }
}
