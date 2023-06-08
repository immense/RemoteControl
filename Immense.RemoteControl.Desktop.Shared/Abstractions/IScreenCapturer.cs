using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Models;
using SkiaSharp;
using System.Drawing;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IScreenCapturer : IDisposable
{
    event EventHandler<Rectangle> ScreenChanged;

    bool CaptureFullscreen { get; set; }
    Rectangle CurrentScreenBounds { get; }
    bool IsGpuAccelerated { get; }
    string SelectedScreen { get; }
    IEnumerable<string> GetDisplayNames();
    SKRect GetFrameDiffArea();

    Result<SKBitmap> GetImageDiff();

    Result<CapturedFrame> GetNextFrame();

    int GetScreenCount();

    int GetSelectedScreenIndex();
    Rectangle GetVirtualScreenBounds();

    void Init();

    void SetSelectedScreen(string displayName);
}
