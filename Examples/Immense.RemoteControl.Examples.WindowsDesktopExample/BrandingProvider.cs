using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;

namespace Immense.RemoteControl.Examples.WindowsDesktopExample;

internal class BrandingProvider : IBrandingProvider
{
    private BrandingInfoBase _brandingInfo = new()
    {
        Product = "Immy Remote"
    };


    public BrandingInfoBase CurrentBranding => _brandingInfo;

    public Task<BrandingInfoBase> GetBrandingInfo()
    {
        return Task.FromResult(_brandingInfo);
    }

    public async Task Initialize()
    {
        using var mrs = typeof(BrandingProvider).Assembly.GetManifestResourceStream("Immense.RemoteControl.Examples.WindowsDesktopExample.ImmyBot.png");
        using var ms = new MemoryStream();
        await mrs!.CopyToAsync(ms);

        _brandingInfo.Icon = ms.ToArray();
    }

    public void SetBrandingInfo(BrandingInfoBase brandingInfo)
    {
        _brandingInfo = brandingInfo;
    }
}
