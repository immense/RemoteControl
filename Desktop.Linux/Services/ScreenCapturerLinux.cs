﻿using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Native.Linux;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Shared;

using Microsoft.Extensions.Logging;

using SkiaSharp;

using System.Drawing;
using System.Runtime.InteropServices;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class ScreenCapturerLinux : IScreenCapturer
{
    private readonly IImageHelper _imageHelper;
    private readonly ILogger<ScreenCapturerLinux> _logger;
    private readonly object _screenBoundsLock = new();
    private readonly Dictionary<string, LibXrandr.XRRMonitorInfo> _x11Screens = new();
    private SKBitmap? _currentFrame;
    private SKBitmap? _previousFrame;

    public ScreenCapturerLinux(
        IImageHelper imageHelper,
        ILogger<ScreenCapturerLinux> logger)
    {
        _imageHelper = imageHelper;
        _logger = logger;
        Display = LibX11.XOpenDisplay(string.Empty);
        Init();
    }

    public event EventHandler<Rectangle>? ScreenChanged;

    public bool CaptureFullscreen { get; set; } = true;
    public Rectangle CurrentScreenBounds { get; private set; }
    public IntPtr Display { get; private set; }
    public string SelectedScreen { get; private set; } = string.Empty;

    public void Dispose()
    {
        LibX11.XCloseDisplay(Display);
        GC.SuppressFinalize(this);
    }
    public IEnumerable<string> GetDisplayNames()
    {
        return _x11Screens.Keys.Select(x => x.ToString());
    }

    public SKRect GetFrameDiffArea()
    {
        if (_currentFrame is null)
        {
            return SKRect.Empty;
        }

        return _imageHelper.GetDiffArea(_currentFrame, _previousFrame, CaptureFullscreen);
    }

    public Result<SKBitmap> GetImageDiff()
    {
        if (_currentFrame is null)
        {
            return Result.Fail<SKBitmap>("Current frame is null.");
        }

        return _imageHelper.GetImageDiff(_currentFrame, _previousFrame);
    }

    public Result<SKBitmap> GetNextFrame()
    {
        lock (_screenBoundsLock)
        {
            try
            {
                if (_currentFrame != null)
                {
                    _previousFrame?.Dispose();
                    _previousFrame = _currentFrame;
                }

                _currentFrame = GetX11Capture();
                return Result.Ok(_currentFrame);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting next frame.");
                Init();
                return Result.Fail<SKBitmap>(ex);
            }
        }
    }
    public int GetScreenCount()
    {
        return _x11Screens.Count;
    }

    public int GetSelectedScreenIndex()
    {
        return int.Parse(SelectedScreen ?? "0");
    }

    public Rectangle GetVirtualScreenBounds()
    {
        int width = 0;
        for (var i = 0; i < GetScreenCount(); i++)
        {
            width += LibX11.XWidthOfScreen(LibX11.XScreenOfDisplay(Display, i));
        }
        int height = 0;
        for (var i = 0; i < GetScreenCount(); i++)
        {
            height += LibX11.XHeightOfScreen(LibX11.XScreenOfDisplay(Display, i));
        }
        return new Rectangle(0, 0, width, height);
    }

    public void Init()
    {
        try
        {
            CaptureFullscreen = true;
            _x11Screens.Clear();

            var monitorsPtr = LibXrandr.XRRGetMonitors(Display, LibX11.XDefaultRootWindow(Display), true, out var monitorCount);

            var monitorInfoSize = Marshal.SizeOf<LibXrandr.XRRMonitorInfo>();

            for (var i = 0; i < monitorCount; i++)
            {
                var monitorPtr = new IntPtr(monitorsPtr.ToInt64() + i * monitorInfoSize);
                var monitorInfo = Marshal.PtrToStructure<LibXrandr.XRRMonitorInfo>(monitorPtr);

                _logger.LogInformation($"Found monitor: " +
                    $"{monitorInfo.width}," +
                    $"{monitorInfo.height}," +
                    $"{monitorInfo.x}, " +
                    $"{monitorInfo.y}");

                _x11Screens.Add(i.ToString(), monitorInfo);
            }

            LibXrandr.XRRFreeMonitors(monitorsPtr);

            if (string.IsNullOrWhiteSpace(SelectedScreen) ||
                !_x11Screens.ContainsKey(SelectedScreen))
            {
                SelectedScreen = _x11Screens.Keys.First();
                RefreshCurrentScreenBounds();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while initializing.");
        }
    }
    public void SetSelectedScreen(string displayName)
    {
        lock (_screenBoundsLock)
        {
            try
            {
                _logger.LogInformation("Setting display to {displayName}.", displayName);
                if (displayName == SelectedScreen)
                {
                    return;
                }
                if (_x11Screens.ContainsKey(displayName))
                {
                    SelectedScreen = displayName;
                }
                else
                {
                    SelectedScreen = _x11Screens.Keys.First();
                }

                RefreshCurrentScreenBounds();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while setting selected display.");
            }
        }
    }

    private SKBitmap GetX11Capture()
    {
        var currentFrame = new SKBitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height);

        var window = LibX11.XDefaultRootWindow(Display);

        var imagePointer = LibX11.XGetImage(Display,
            window,
            CurrentScreenBounds.X,
            CurrentScreenBounds.Y,
            CurrentScreenBounds.Width,
            CurrentScreenBounds.Height,
            ~0,
            2);

        var image = Marshal.PtrToStructure<LibX11.XImage>(imagePointer);

        var pixels = currentFrame.GetPixels();
        unsafe
        {
            byte* scan1 = (byte*)pixels.ToPointer();
            byte* scan2 = (byte*)image.data.ToPointer();
            var bytesPerPixel = currentFrame.BytesPerPixel;
            var totalSize = currentFrame.Height * currentFrame.Width * bytesPerPixel;
            for (int counter = 0; counter < totalSize - bytesPerPixel; counter++)
            {
                scan1[counter] = scan2[counter];
            }
        }

        Marshal.DestroyStructure<LibX11.XImage>(imagePointer);
        LibX11.XDestroyImage(imagePointer);

        return currentFrame;
    }

    private void RefreshCurrentScreenBounds()
    {
        var screen = _x11Screens[SelectedScreen];

        _logger.LogInformation($"Setting new screen bounds: " +
             $"{screen.width}," +
             $"{screen.height}," +
             $"{screen.x}, " +
             $"{screen.y}");

        CurrentScreenBounds = new Rectangle(screen.x, screen.y, screen.width, screen.height);
        CaptureFullscreen = true;
        ScreenChanged?.Invoke(this, CurrentScreenBounds);
    }
}
