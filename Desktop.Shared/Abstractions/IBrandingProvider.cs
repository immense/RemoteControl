using Immense.RemoteControl.Shared.Models;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IBrandingProvider
{
    Task<BrandingInfoBase> GetBrandingInfo();
    void SetBrandingInfo(BrandingInfoBase brandingInfo);
}
