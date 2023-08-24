using System.ComponentModel.DataAnnotations;

namespace Immense.RemoteControl.Shared.Models;

public class BrandingInfoBase
{
    public static BrandingInfoBase Default => new();

    public byte[] Icon { get; set; } = Array.Empty<byte>();

    [StringLength(25)]
    public string Product { get; set; } = "Remote Control";
}
