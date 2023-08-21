using Immense.RemoteControl.Desktop.Services;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.ViewModels;

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
