using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Shared.Models;

public class CapturedFrame
{
    public CapturedFrame(SKBitmap screenCapture, IEnumerable<SKRect> changedRegions)
    {
        ScreenCapture = screenCapture;
        ChangedRegions = changedRegions;
    }

    public IEnumerable<SKRect> ChangedRegions { get; init; }
    public SKBitmap ScreenCapture { get; init; }
}
