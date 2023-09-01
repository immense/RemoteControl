using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Windows.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using System.Diagnostics;

namespace Immense.RemoteControl.Desktop.WIndows.Tests;

//[TestClass]
public class EncodingBenchmarks
{

    [TestMethod]
    public async Task CaptureDiff()
    {
        var imageHelper = new ImageHelper(Mock.Of<ILogger<ImageHelper>>());
        var capturer = new ScreenCapturerWin(imageHelper, Mock.Of<ILogger<ScreenCapturerWin>>());

        var totalFrames = 0;

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < 5)
        {
            var result = capturer.GetNextFrame();
            if (!result.IsSuccess)
            {
                continue;
            }
            if (!result.IsSuccess)
            {
                await Task.Yield();
                continue;
            }

            _ = capturer.GetFrameDiffArea();

            totalFrames++;
        }

        var fps = (double)totalFrames / 5;
        Console.WriteLine($"FPS: {fps}");
    }



    [TestMethod]
    public async Task CaptureDiffCropEncode()
    {
        var imageHelper = new ImageHelper(Mock.Of<ILogger<ImageHelper>>());
        var capturer = new ScreenCapturerWin(imageHelper, Mock.Of<ILogger<ScreenCapturerWin>>());

        var totalBytesSent = 0;
        var totalFrames = 0;

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < 5)
        {
            var result = capturer.GetNextFrame();
            if (!result.IsSuccess)
            {
                continue;
            }
            if (!result.IsSuccess)
            {
                await Task.Yield();
                continue;
            }

            var diffArea = capturer.GetFrameDiffArea();

            if (diffArea.IsEmpty)
            {
                await Task.Yield();
                continue;
            }

            capturer.CaptureFullscreen = false;

            using var croppedFrame = imageHelper.CropBitmap(result.Value, diffArea);

            var encodedImageBytes = imageHelper.EncodeBitmap(croppedFrame, SKEncodedImageFormat.Jpeg, 80);

            if (encodedImageBytes.Length == 0)
            {
                continue;
            }

            totalBytesSent += encodedImageBytes.Length;
            totalFrames++;
        }

        var fps = (double)totalFrames / 5;
        Console.WriteLine($"FPS: {fps}");

        var megabytes = (double)totalBytesSent / 1024 / 1024;
        Console.WriteLine($"Total MB: {megabytes:N2}");

        var mbps = (double)totalBytesSent / 5 / 1024 / 1024 * 8;
        Console.WriteLine($"Mbps: {mbps}");
    }

}

