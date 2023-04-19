using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;

namespace Immense.RemoteControl.Examples.WindowsDesktopExample;

internal class BrandingProvider : IBrandingProvider
{
    private BrandingInfoBase _brandingInfo = new()
    {
        Product = "Immy Remote"
    };

    public BrandingProvider()
    {
        using var mrs = typeof(BrandingProvider).Assembly.GetManifestResourceStream("Immense.RemoteControl.Examples.WindowsDesktopExample.ImmyBot.png");
        using var ms = new MemoryStream();
        mrs!.CopyTo(ms);

        _brandingInfo.Icon = ms.ToArray();
    }

    public Task<BrandingInfoBase> GetBrandingInfo()
    {
        return Task.FromResult(_brandingInfo);
    }

    public void SetBrandingInfo(BrandingInfoBase brandingInfo)
    {
        _brandingInfo = brandingInfo;
    }
}
