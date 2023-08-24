using Immense.RemoteControl.Shared.Models;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IBrandingProvider
{
    BrandingInfoBase CurrentBranding { get; }
    Task Initialize();
    void SetBrandingInfo(BrandingInfoBase brandingInfo);
}
