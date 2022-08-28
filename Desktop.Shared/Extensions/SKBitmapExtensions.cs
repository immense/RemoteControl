using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Extensions
{
    public static class SKBitmapExtensions
    {
        public static SKRect ToRectangle(this SKBitmap bitmap)
        {
            return new SKRect(0, 0, bitmap.Width, bitmap.Height);
        }
    }
}
