using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktopExample
{
    internal class BrandingProvider : IBrandingProvider
    {
        private BrandingInfo _brandingInfo = new();

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
