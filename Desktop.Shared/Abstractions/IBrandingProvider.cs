using Remotely.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions
{
    public interface IBrandingProvider
    {
        Task<BrandingInfo> GetBrandingInfo();
        void SetBrandingInfo(BrandingInfo brandingInfo);
    }
}
