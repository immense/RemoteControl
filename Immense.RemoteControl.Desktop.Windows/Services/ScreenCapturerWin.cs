﻿// The DirectX capture code is based off examples from the
// SharpDX Samples at https://github.com/sharpdx/SharpDX.

// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Messages;
using Immense.RemoteControl.Desktop.Shared.Native.Windows;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Windows.Helpers;
using Immense.RemoteControl.Desktop.Windows.Models;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Extensions;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Models;
using Immense.SimpleMessenger;
using Microsoft.Extensions.Logging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Result = Immense.RemoteControl.Shared.Result;

namespace Immense.RemoteControl.Desktop.Windows.Services;

[SupportedOSPlatform("windows")]
public class ScreenCapturerWin : IScreenCapturer
{
    private readonly Dictionary<string, DirectXOutput> _directxScreens = new();
    private readonly ConcurrentDictionary<string, DisplayInfo> _displays = new();
    private readonly IImageHelper _imageHelper;
    private readonly ILogger<ScreenCapturerWin> _logger;
    private readonly object _screenBoundsLock = new();
    private SKBitmap? _currentFrame;
    private bool _needsInit;
    private SKBitmap? _previousFrame;

    public ScreenCapturerWin(
        IImageHelper imageHelper,
        IMessenger messenger,
        ILogger<ScreenCapturerWin> logger)
    {
        _imageHelper = imageHelper;
        _logger = logger;

        Init();

        // Registration is automatically removed when subscriber is disposed.
        _ = messenger.Register<DisplaySettingsChangedMessage>(this, HandleDisplaySettingsChanged);
    }

    public event EventHandler<Rectangle>? ScreenChanged;

    public bool CaptureFullscreen { get; set; } = true;

    public Rectangle CurrentScreenBounds { get; private set; }

    public bool IsGpuAccelerated { get; private set; }

    public string SelectedScreen { get; private set; } = string.Empty;

    private SKBitmap? CurrentFrame
    {
        get => _currentFrame;
        set
        {
            if (_currentFrame != null)
            {
                _previousFrame?.Dispose();
                _previousFrame = _currentFrame;
            }
            _currentFrame = value;
        }
    }

    public void Dispose()
    {
        try
        {
            ClearDirectXOutputs();
            GC.SuppressFinalize(this);
        }
        catch { }
    }

    public IEnumerable<string> GetDisplayNames()
    {
        return DisplaysEnumerationHelper
            .GetDisplays()
            .Select(x => x.DeviceName);
    }

    public SKRect GetFrameDiffArea()
    {
        if (CurrentFrame is null)
        {
            return SKRect.Empty;
        }
        return _imageHelper.GetDiffArea(CurrentFrame, _previousFrame, CaptureFullscreen);
    }

    public Result<SKBitmap> GetImageDiff()
    {

        if (CurrentFrame is null)
        {
            return Result.Fail<SKBitmap>("Current frame cannot be empty.");
        }
        return _imageHelper.GetImageDiff(CurrentFrame, _previousFrame);
    }

    public Result<SKBitmap> GetNextFrame()
    {
        lock (_screenBoundsLock)
        {
            try
            {
                if (!Win32Interop.SwitchToInputDesktop())
                {
                    // Something will occasionally prevent this from succeeding after active
                    // desktop has changed to/from WinLogon (err code 170).  I'm guessing a hook
                    // is getting put in the desktop, which causes SetThreadDesktop to fail.
                    // The caller can start a new thread, which seems to resolve it.
                    var errCode = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to switch to input desktop. Last Win32 error code: {errCode}", errCode);
                    return Result.Fail<SKBitmap>($"Failed to switch to input desktop. Last Win32 error code: {errCode}");
                }

                if (_needsInit)
                {
                    _logger.LogWarning("Init needed in GetNextFrame.");
                    Init();
                }

                if (Process.GetCurrentProcess().SessionId == 0)
                {
                    var backstageResult = GetBackstageFrame();
                    if (!backstageResult.IsSuccess)
                    {
                        var ex = backstageResult.Exception ??
                            new("Unknown error while getting backstage frame.");
                        _logger.LogError(ex, "Error while getting backstage frame.");
                        return Result.Fail<SKBitmap>(ex);
                    }
                    CurrentFrame = backstageResult.Value;
                    return Result.Ok(CurrentFrame);
                }

                var result = GetDirectXFrame();

                if (result.IsSuccess && !result.HadChanges)
                {
                    return Result.Fail<SKBitmap>("No screen changes occurred.");
                }

                if (result.HadChanges && !IsEmpty(result.Bitmap))
                {
                    CurrentFrame = result.Bitmap;
                    return Result.Ok(CurrentFrame);
                }

                var bitBltResult = GetBitBltFrame();
                if (!bitBltResult.IsSuccess)
                {
                    var ex = bitBltResult.Exception ?? new("Unknown error.");
                    _logger.LogError(ex, "Error while getting next frame.");
                    return Result.Fail<SKBitmap>(ex);
                }
                CurrentFrame = bitBltResult.Value;

                return Result.Ok(CurrentFrame);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting next frame.");
                _needsInit = true;
                return Result.Fail<SKBitmap>(e);
            }
        }
    }

    public int GetScreenCount()
    {
        return DisplaysEnumerationHelper.GetDisplays().Count();
    }

    public Rectangle GetVirtualScreenBounds()
    {
        var displays = DisplaysEnumerationHelper.GetDisplays();
        var lowestX = 0;
        var highestX = 0;
        var lowestY = 0;
        var highestY = 0;

        foreach (var display in displays)
        {
            lowestX = Math.Min(display.MonitorArea.Left, lowestX);
            highestX = Math.Max(display.MonitorArea.Right, highestX);
            lowestY = Math.Min(display.MonitorArea.Top, lowestY);
            highestY = Math.Max(display.MonitorArea.Bottom, highestY);
        }

        return new Rectangle(lowestX, lowestY, highestX - lowestX, highestY - lowestY);
    }

    public void Init()
    {
        Win32Interop.SwitchToInputDesktop();

        CaptureFullscreen = true;
        InitDisplays();
        InitDirectX();

        ScreenChanged?.Invoke(this, CurrentScreenBounds);

        _needsInit = false;
    }

    public void SetSelectedScreen(string displayName)
    {
        lock (_screenBoundsLock)
        {
            if (displayName == SelectedScreen)
            {
                return;
            }

            if (!_displays.TryGetValue(displayName, out var display))
            {
                display = _displays.First().Value;
            }

            SelectedScreen = displayName;
            CurrentScreenBounds = display.MonitorArea;
            CaptureFullscreen = true;
            ScreenChanged?.Invoke(this, CurrentScreenBounds);
        }
    }

    internal Result<SKBitmap> GetBitBltFrame()
    {
        var hwnd = nint.Zero;
        var screenDc = nint.Zero;

        try
        {
            hwnd = User32.GetDesktopWindow();
            screenDc = User32.GetWindowDC(hwnd);
            using var bitmap = new Bitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            var targetDc = graphics.GetHdc();

            GDI32.BitBlt(
                targetDc, 0, 0, bitmap.Width, bitmap.Height,
                screenDc, 0, 0, GDI32.TernaryRasterOperations.SRCCOPY);

            graphics.ReleaseHdc(targetDc);

            return Result.Ok(bitmap.ToSKBitmap());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capturer error in BitBltCapture.");
            _needsInit = true;
            return Result.Fail<SKBitmap>("Error while capturing BitBlt frame.");
        }
        finally
        {
            _ = User32.ReleaseDC(hwnd, screenDc);
        }
    }

    [Obsolete($"Use {nameof(GetBitBltFrame)}.")]
    internal Result<SKBitmap> GetBitBltFrameOld()
    {
        try
        {
            using var bitmap = new Bitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height, PixelFormat.Format32bppArgb);
            using (var graphic = Graphics.FromImage(bitmap))
            {
                graphic.CopyFromScreen(CurrentScreenBounds.Left, CurrentScreenBounds.Top, 0, 0, new Size(CurrentScreenBounds.Width, CurrentScreenBounds.Height));
            }
            return Result.Ok(bitmap.ToSKBitmap());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capturer error in BitBltCapture.");
            _needsInit = true;
            return Result.Fail<SKBitmap>("Error while capturing BitBlt frame.");
        }
    }

    private void ClearDirectXOutputs()
    {
        foreach (var screen in _directxScreens.Values)
        {
            try
            {
                screen.Dispose();
            }
            catch { }
        }
        _directxScreens.Clear();
    }

    private Result<SKBitmap> GetBackstageFrame()
    {
        try
        {
            PrintDebugInfo();

            using var bitmap = new Bitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            var targetDc = graphics.GetHdc();

            var windowList = new List<nint>();

            var enumResult = User32.EnumWindows((
                hwnd, lparam) =>
                {
                    windowList.Add(hwnd);
                    return true;
                },
                nint.Zero);

            windowList.Reverse();

            if (!enumResult)
            {
                var result = Result.Fail<SKBitmap>(
                    $"Failed to enumerate desktop windows. Last error code: {Marshal.GetLastPInvokeError()}");
                _logger.LogResult(result);
                return result;
            }


            foreach (var hwnd in windowList)
            {
                try
                {
                    var winInfo = new User32.WINDOWINFO();
                    winInfo.cbSize = (uint)Marshal.SizeOf(winInfo);

                    if (!User32.GetWindowInfo(hwnd, ref winInfo))
                    {
                        _logger.LogError("Failed to get window info.");
                        continue;
                    }

                    var windowDc = User32.GetWindowDC(hwnd);

                    var winRect = new Rectangle(
                        winInfo.rcWindow.Left,
                        winInfo.rcWindow.Top,
                        winInfo.rcWindow.Right - winInfo.rcWindow.Left,
                        winInfo.rcWindow.Bottom - winInfo.rcWindow.Top);

                    GDI32.BitBlt(
                       targetDc, winRect.Left, winRect.Top, winRect.Width, winRect.Height,
                       windowDc, 0, 0, GDI32.TernaryRasterOperations.SRCCOPY);

                    User32.ReleaseDC(hwnd, windowDc);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while calling BitBlt on child window.");
                }
            }

            graphics.ReleaseHdc(targetDc);

            return Result.Ok(bitmap.ToSKBitmap());
        }
        catch (Exception ex)
        {
            const string err = "Error while capturing backstage frame.";
            _logger.LogError(ex, err);
            return Result.Fail<SKBitmap>(err);
        }
    }

    private DxCaptureResult GetDirectXFrame()
    {
        if (!_directxScreens.TryGetValue(SelectedScreen, out var dxOutput))
        {
            return DxCaptureResult.Fail("DirectX output not found.");
        }

        try
        {
            var outputDuplication = dxOutput.OutputDuplication;
            var device = dxOutput.Device;
            var texture2D = dxOutput.Texture2D;
            var bounds = dxOutput.Bounds;

            var result = outputDuplication.TryAcquireNextFrame(timeoutInMilliseconds: 25, out var duplicateFrameInfo, out var screenResource);

            if (!result.Success)
            {
                return DxCaptureResult.TryAcquireFailed(result);
            }

            if (duplicateFrameInfo.AccumulatedFrames == 0)
            {
                try
                {
                    outputDuplication.ReleaseFrame();
                }
                catch { }
                return DxCaptureResult.NoAccumulatedFrames(result);
            }

            using Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>();
            device.ImmediateContext.CopyResource(screenTexture2D, texture2D);
            var dataBox = device.ImmediateContext.MapSubresource(texture2D, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var dataBoxPointer = dataBox.DataPointer;
            var bitmapDataPointer = bitmapData.Scan0;
            for (var y = 0; y < bounds.Height; y++)
            {
                Utilities.CopyMemory(bitmapDataPointer, dataBoxPointer, bounds.Width * 4);
                dataBoxPointer = nint.Add(dataBoxPointer, dataBox.RowPitch);
                bitmapDataPointer = nint.Add(bitmapDataPointer, bitmapData.Stride);
            }
            bitmap.UnlockBits(bitmapData);
            device.ImmediateContext.UnmapSubresource(texture2D, 0);
            screenResource?.Dispose();

            switch (dxOutput.Rotation)
            {
                case DisplayModeRotation.Unspecified:
                case DisplayModeRotation.Identity:
                    break;
                case DisplayModeRotation.Rotate90:
                    bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
                case DisplayModeRotation.Rotate180:
                    bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case DisplayModeRotation.Rotate270:
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                default:
                    break;
            }
            IsGpuAccelerated = true;
            return DxCaptureResult.Ok(bitmap.ToSKBitmap(), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting DirectX frame.");
            IsGpuAccelerated = false;
        }
        finally
        {
            try
            {
                dxOutput.OutputDuplication.ReleaseFrame();
            }
            catch { }
        }

        return DxCaptureResult.Fail("Failed to get DirectX frame.");
    }

    private Task HandleDisplaySettingsChanged(DisplaySettingsChangedMessage message)
    {
        _needsInit = true;
        return Task.CompletedTask;
    }

    private void InitDirectX()
    {
        try
        {
            ClearDirectXOutputs();

            using var factory = new Factory1();
            foreach (var adapter in factory.Adapters1.Where(x => (x.Outputs?.Length ?? 0) > 0))
            {
                foreach (var output in adapter.Outputs)
                {
                    try
                    {
                        var device = new SharpDX.Direct3D11.Device(adapter);
                        var output1 = output.QueryInterface<Output1>();

                        var bounds = output1.Description.DesktopBounds;
                        var width = bounds.Right - bounds.Left;
                        var height = bounds.Bottom - bounds.Top;

                        // Create Staging texture CPU-accessible
                        var textureDesc = new Texture2DDescription
                        {
                            CpuAccessFlags = CpuAccessFlags.Read,
                            BindFlags = BindFlags.None,
                            Format = Format.B8G8R8A8_UNorm,
                            Width = width,
                            Height = height,
                            OptionFlags = ResourceOptionFlags.None,
                            MipLevels = 1,
                            ArraySize = 1,
                            SampleDescription = { Count = 1, Quality = 0 },
                            Usage = ResourceUsage.Staging
                        };

                        var texture2D = new Texture2D(device, textureDesc);

                        _directxScreens.Add(
                            output1.Description.DeviceName,
                            new DirectXOutput(adapter,
                                device,
                                output1.DuplicateOutput(device),
                                texture2D,
                                output1.Description.Rotation));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while initializing DirectX.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while initializing DirectX.");
        }
    }

    private void InitDisplays()
    {
        _displays.Clear();

        var displays = DisplaysEnumerationHelper.GetDisplays().ToArray();
        foreach (var display in displays)
        {
            _displays.AddOrUpdate(display.DeviceName, display, (k, v) => display);
        }

        var primary = displays.FirstOrDefault(x => x.IsPrimary) ?? displays.First();
        SelectedScreen = primary.DeviceName;
        CurrentScreenBounds = primary.MonitorArea;

        _logger.LogInformation("Found {count} displays.", displays.Length);
        _logger.LogInformation("Current bounds: {bounds}", JsonSerializer.Serialize(CurrentScreenBounds));
    }

    private bool IsEmpty(SKBitmap bitmap)
    {
        if (bitmap is null)
        {
            return true;
        }

        var height = bitmap.Height;
        var width = bitmap.Width;
        var bytesPerPixel = bitmap.BytesPerPixel;

        try
        {
            unsafe
            {
                byte* scan = (byte*)bitmap.GetPixels();

                for (var row = 0; row < height; row++)
                {
                    for (var column = 0; column < width; column++)
                    {
                        var index = row * width * bytesPerPixel + column * bytesPerPixel;

                        byte* data = scan + index;

                        for (var i = 0; i < bytesPerPixel; i++)
                        {
                            if (data[i] != 0)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }
        catch
        {
            return true;
        }
    }

    // TODO:  Remove when done testing.
    private void PrintDebugInfo()
    {
        _ = RateLimiter.Throttle(
            () =>
            {
                if (Win32Interop.GetCurrentDesktopName(out var currentDesktop))
                {
                    _logger.LogInformation("Current Desktop: {desktopName}", currentDesktop);
                }
                else
                {
                    _logger.LogError("Failed to get current desktop.");
                }

                if (Win32Interop.GetInputDesktopName(out var inputDesktop))
                {
                    _logger.LogInformation("Input Desktop: {desktopName}", currentDesktop);
                }
                else
                {
                    _logger.LogError("Failed to get input desktop.");
                }

                //var desktopHwnd = Win32Interop.OpenInputDesktop();
                //var desktopHwnd = User32.GetDesktopWindow();
                //if (desktopHwnd == nint.Zero)
                //{
                //    _logger.LogError("Failed to get desktop window.");
                //}

                //var notepad = Process.GetProcessesByName("notepad").FirstOrDefault();
                //if (notepad is null)
                //{
                //    _logger.LogError("Notepad not found.");
                //}
                //else
                //{
                //    _logger.LogInformation("Notepad hwnd: {hwnd}", notepad.MainWindowHandle.ToInt64());
                //    var parentHwnd = User32.GetParent(notepad.MainWindowHandle);
                //    _logger.LogInformation("Notepad parent: {parentHwnd}", parentHwnd.ToInt64());
                //    _logger.LogInformation("Desktop window: {desktopHwnd}", desktopHwnd.ToInt64());

                //    var setParentResult = User32.SetParent(notepad.MainWindowHandle, desktopHwnd);
                //    User32.SetWindowPos(notepad.MainWindowHandle, 0, 0, 0, 400, 400, User32.SWP.SWP_SHOWWINDOW | User32.SWP.SWP_NOSIZE);
                //    _logger.LogInformation("Set parent result: {result}", setParentResult.ToInt64());
                //    parentHwnd = User32.GetParent(notepad.MainWindowHandle);
                //    _logger.LogInformation("Notepad parent: {parentHwnd}", parentHwnd.ToInt64());
                //}

                //var enumResult = User32.EnumWindows((hwnd, lParam) =>
                //{
                //    var winInfo = new User32.WINDOWINFO();
                //    winInfo.cbSize = (uint)Marshal.SizeOf(winInfo);

                //    if (!User32.GetWindowInfo(hwnd, ref winInfo))
                //    {
                //        _logger.LogError("Failed to get window info.");
                //    }

                //    var options = new JsonSerializerOptions() { IncludeFields = true };
                //    _logger.LogInformation("Get window info: {info}", JsonSerializer.Serialize(winInfo, options));

                //    return true;
                //}, IntPtr.Zero);

                //if (!enumResult)
                //{
                //    _logger.LogError("Failed to enumerate desktop windows. Last error code: {code}", Marshal.GetLastPInvokeError());
                //}

                return Task.CompletedTask;
            },
            TimeSpan.FromSeconds(10));
    }
    private class DxCaptureResult
    {
        public SKBitmap? Bitmap { get; init; }
        public SharpDX.Result? DxResult { get; init; }
        public string FailureReason { get; init; } = string.Empty;

        [MemberNotNull(nameof(Bitmap))]
        public bool HadChanges { get; init; }

        public bool IsSuccess { get; init; }

        internal static DxCaptureResult Fail(string failureReason)
        {
            return new DxCaptureResult()
            {
                FailureReason = failureReason
            };
        }

        internal static DxCaptureResult Fail(string failureReason, SharpDX.Result dxResult)
        {
            return new DxCaptureResult()
            {
                FailureReason = failureReason,
                DxResult = dxResult
            };
        }

        internal static DxCaptureResult NoAccumulatedFrames(SharpDX.Result dxResult)
        {
            return new DxCaptureResult()
            {
                FailureReason = "No frames were accumulated.",
                DxResult = dxResult,
                IsSuccess = true
            };
        }

        internal static DxCaptureResult Ok(SKBitmap sKBitmap, SharpDX.Result result)
        {
            return new DxCaptureResult()
            {
                Bitmap = sKBitmap,
                DxResult = result,
                HadChanges = true,
                IsSuccess = true,
            };
        }

        internal static DxCaptureResult TryAcquireFailed(SharpDX.Result dxResult)
        {
            if (dxResult.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Code)
            {
                return new DxCaptureResult()
                {
                    FailureReason = "Timed out while waiting for the next frame.",
                    DxResult = dxResult,
                    IsSuccess = true
                };
            }
            return new DxCaptureResult()
            {
                FailureReason = "TryAcquireFrame returned failure.",
                DxResult = dxResult
            };
        }
    }
}
