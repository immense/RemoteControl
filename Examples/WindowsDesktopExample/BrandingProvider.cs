using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktopExample
{
    internal class BrandingProvider : IBrandingProvider
    {
        private BrandingInfo _brandingInfo = new()
        {
            Product = "Immy Remote"
        };

        public BrandingProvider()
        {
            using var mrs = typeof(BrandingProvider).Assembly.GetManifestResourceStream("WindowsDesktopExample.ImmyBot.png");
            using var ms = new MemoryStream();
            mrs!.CopyTo(ms);

            _brandingInfo.Icon = ms.ToArray();
        }

        public Task<BrandingInfo> GetBrandingInfo()
        {
            return Task.FromResult(_brandingInfo);
        }

        public void SetBrandingInfo(BrandingInfo brandingInfo)
        {
            _brandingInfo = brandingInfo;
        }
    }
}
