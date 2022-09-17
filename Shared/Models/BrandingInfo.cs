using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Shared.Models
{
    public class BrandingInfoBase
    {
        public static BrandingInfoBase Default => new();

        public byte[] Icon { get; set; } = Array.Empty<byte>();

        [StringLength(25)]
        public string Product { get; set; } = "Remote Control";

        public byte TitleForegroundRed { get; set; } = 29;

        public byte TitleForegroundGreen { get; set; } = 144;

        public byte TitleForegroundBlue { get; set; } = 241;

        public byte TitleBackgroundRed { get; set; } = 70;

        public byte TitleBackgroundGreen { get; set; } = 70;

        public byte TitleBackgroundBlue { get; set; } = 70;

        public byte ButtonForegroundRed { get; set; } = 255;

        public byte ButtonForegroundGreen { get; set; } = 255;

        public byte ButtonForegroundBlue { get; set; } = 255;
    }
}
