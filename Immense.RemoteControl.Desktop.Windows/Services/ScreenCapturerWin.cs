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

using Microsoft.Win32;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Drawing.Imaging;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Runtime.InteropServices;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Shared;
using Result = Immense.RemoteControl.Shared.Result;
using Immense.RemoteControl.Desktop.Windows.Models;
using Immense.RemoteControl.Desktop.Native.Windows;
using System.Diagnostics.CodeAnalysis;
using SharpDX.Mathematics.Interop;
using Immense.RemoteControl.Shared.Models;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace Immense.RemoteControl.Desktop.Windows.Services;

public class ScreenCapturerWin : IScreenCapturer
{
    private readonly Dictionary<string, int> _bitBltScreens = new();
    private readonly Dictionary<string, DirectXOutput> _directxScreens = new();
    private readonly IImageHelper _imageHelper;
    private readonly ILogger<ScreenCapturerWin> _logger;
    private readonly object _screenBoundsLock = new();
    private readonly int _rawRectangleSize = Marshal.SizeOf<RawRectangle>();

    private SKBitmap? _currentFrame;
    private bool _needsInit;
    private SKBitmap? _previousFrame;
    public ScreenCapturerWin(
        IImageHelper imageHelper,
        ILogger<ScreenCapturerWin> logger)
    {
        _imageHelper = imageHelper;
        _logger = logger;

        Init();
        
        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    public event EventHandler<Rectangle>? ScreenChanged;

    public bool CaptureFullscreen { get; set; } = true;
    public Rectangle CurrentScreenBounds { get; private set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
    public bool IsGpuAccelerated { get; private set; }
    public string SelectedScreen { get; private set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;
    public void Dispose()
    {
        try
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            ClearDirectXOutputs();
            GC.SuppressFinalize(this);
        }
        catch { }
    }
    public IEnumerable<string> GetDisplayNames()
    {
        return Screen.AllScreens.Select(x => x.DeviceName);
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

    public Result<CapturedFrame> GetNextFrame()
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
                    return Result.Fail<CapturedFrame>($"Failed to switch to input desktop. Last Win32 error code: {errCode}");
                }

                if (_needsInit)
                {
                    _logger.LogWarning("Init needed in GetNextFrame.");
                    Init();
                }

                var result = GetDirectXFrame();

                if (result.IsSuccess && !result.HadChanges)
                {
                    return Result.Fail<CapturedFrame>("No screen changes occurred.");
                }

                if (result.HadChanges && !IsEmpty(result.Bitmap))
                {
                    CurrentFrame = result.Bitmap;
                }
                else
                {
                    var bitBltResult = GetBitBltFrame();
                    if (!bitBltResult.IsSuccess)
                    {
                        var ex = bitBltResult.Exception ?? new("Unknown error.");
                        _logger.LogError(ex, "Error while getting next frame.");
                        return Result.Fail<CapturedFrame>(ex);
                    }
                    CurrentFrame = bitBltResult.Value;
                }

                return Result.Ok(new CapturedFrame(CurrentFrame, result.ChangedRegions));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while getting next frame.");
                _needsInit = true;
                return Result.Fail<CapturedFrame>(e);
            }
        }
    }

    public int GetScreenCount()
    {
        return Screen.AllScreens.Length;
    }

    public int GetSelectedScreenIndex()
    {
        if (_bitBltScreens.TryGetValue(SelectedScreen, out var index))
        {
            return index;
        }
        return 0;
    }

    public Rectangle GetVirtualScreenBounds()
    {
        return SystemInformation.VirtualScreen;
    }

    public void Init()
    {
        Win32Interop.SwitchToInputDesktop();

        CaptureFullscreen = true;
        InitBitBlt();
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

            if (_bitBltScreens.ContainsKey(displayName))
            {
                SelectedScreen = displayName;
            }
            else
            {
                SelectedScreen = _bitBltScreens.Keys.First();
            }
            RefreshCurrentScreenBounds();
        }
    }

    internal Result<SKBitmap> GetBitBltFrame()
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
                dataBoxPointer = IntPtr.Add(dataBoxPointer, dataBox.RowPitch);
                bitmapDataPointer = IntPtr.Add(bitmapDataPointer, bitmapData.Stride);
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

            var changedRegions = Array.Empty<SKRect>();

            try
            {
                if (duplicateFrameInfo.TotalMetadataBufferSize > 0)
                {
                    var metadataBuffer = new RawRectangle[duplicateFrameInfo.TotalMetadataBufferSize];
                    outputDuplication.GetFrameDirtyRects(duplicateFrameInfo.TotalMetadataBufferSize, metadataBuffer, out var dirtySize);

                    var dirtyArraySize = dirtySize / _rawRectangleSize;

                    if (dirtyArraySize < 10)
                    {
                        changedRegions = new SKRect[dirtyArraySize];
                        for (var i = 0; i < dirtyArraySize; i++)
                        {
                            var rect = metadataBuffer[i];
                            changedRegions[i] = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
                        }
                    }
                }

                Debug.WriteLine($"Changed regions: {changedRegions.Length}");
            }
            catch { }

            return DxCaptureResult.Ok(bitmap.ToSKBitmap(), result, changedRegions);
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

    private void InitBitBlt()
    {
        _bitBltScreens.Clear();
        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            _bitBltScreens.Add(Screen.AllScreens[i].DeviceName, i);
        }
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

    private void RefreshCurrentScreenBounds()
    {
        CurrentScreenBounds = Screen.AllScreens[_bitBltScreens[SelectedScreen]].Bounds;
        CaptureFullscreen = true;
        _needsInit = true;
        ScreenChanged?.Invoke(this, CurrentScreenBounds);
    }

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

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        RefreshCurrentScreenBounds();
    }

    private class DxCaptureResult
    {
        public SKBitmap? Bitmap { get; init; }
        public SharpDX.Result? DxResult { get; init; }
        public string FailureReason { get; init; } = string.Empty;

        [MemberNotNull(nameof(Bitmap))]
        public bool HadChanges { get; init; }

        public bool IsSuccess { get; init; }
        public SKRect[] ChangedRegions { get; init; } = Array.Empty<SKRect>();

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

        internal static DxCaptureResult Ok(
            SKBitmap sKBitmap, 
            SharpDX.Result result,
            SKRect[] changedRegions)
        {
            return new DxCaptureResult()
            {
                Bitmap = sKBitmap,
                DxResult = result,
                HadChanges = true,
                IsSuccess = true,
                ChangedRegions = changedRegions,
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
