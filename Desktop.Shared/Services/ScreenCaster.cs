using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using SkiaSharp;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Models.Dtos;
using MessagePack;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IScreenCaster
    {
        void BeginScreenCasting(ScreenCastRequest screenCastRequest);
    }

    public class ScreenCaster : IScreenCaster
    {
        private static CancellationTokenSource? _metricsCts;
        private readonly IAppState _appState;
        private readonly ICursorIconWatcher _cursorIconWatcher;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger<ScreenCaster> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISessionIndicator _sessionIndicator;
        private readonly IShutdownService _shutdownService;
        private readonly ISystemTime _systemTime;
        public ScreenCaster(
            IAppState appState,
            ICursorIconWatcher cursorIconWatcher,
            ISessionIndicator sessionIndicator,
            IServiceProvider serviceProvider,
            IShutdownService shutdownService,
            IImageHelper imageHelper,
            ISystemTime systemTime,
            ILogger<ScreenCaster> logger)
        {
            _appState = appState;
            _cursorIconWatcher = cursorIconWatcher;
            _sessionIndicator = sessionIndicator;
            _serviceProvider = serviceProvider;
            _shutdownService = shutdownService;
            _imageHelper = imageHelper;
            _systemTime = systemTime;
            _logger = logger;
        }

        public void BeginScreenCasting(ScreenCastRequest screenCastRequest)
        {
            _ = Task.Run(() => BeginScreenCastingImpl(screenCastRequest));
        }

        private async Task BeginScreenCastingImpl(ScreenCastRequest screenCastRequest)
        {
            try
            {
                var viewer = _serviceProvider.GetRequiredService<IViewer>();
                viewer.Name = screenCastRequest.RequesterName;
                viewer.ViewerConnectionID = screenCastRequest.ViewerID;

                var screenBounds = viewer.Capturer.CurrentScreenBounds;

                _logger.LogInformation(
                    "Starting screen cast.  Requester: {viewerName}. " +
                    "Viewer ID: {viewerViewerConnectionID}.  App Mode: {mode}",
                    viewer.Name,
                    viewer.ViewerConnectionID,
                    _appState.Mode);

                _appState.Viewers.AddOrUpdate(viewer.ViewerConnectionID, viewer, (id, v) => viewer);

                if (_appState.Mode == AppMode.Attended)
                {
                    _appState.InvokeViewerAdded(viewer);
                }

                if (_appState.Mode == AppMode.Unattended && screenCastRequest.NotifyUser)
                {
                    _sessionIndicator.Show();
                }

                await viewer.SendViewerConnected();

                await viewer.SendScreenData(
                    viewer.Capturer.SelectedScreen,
                    viewer.Capturer.GetDisplayNames(),
                    screenBounds.Width,
                    screenBounds.Height);

                await viewer.SendCursorChange(_cursorIconWatcher.GetCurrentCursor());

                await viewer.SendWindowsSessions();

                viewer.Capturer.ScreenChanged += async (sender, bounds) =>
                {
                    await viewer.SendScreenSize(bounds.Width, bounds.Height);
                };

                // This gets disposed internally in the Capturer on the next call.
                var result = viewer.Capturer.GetNextFrame();

                if (result.IsSuccess && result.Value is not null)
                {
                    await viewer.SendScreenCapture(new ScreenCaptureDto()
                    {
                        ImageBytes = _imageHelper.EncodeBitmap(result.Value, SKEncodedImageFormat.Jpeg, viewer.ImageQuality),
                        Left = screenBounds.Left,
                        Top = screenBounds.Top,
                        Width = screenBounds.Width,
                        Height = screenBounds.Height
                    });
                }

                // Wait until the first image is received.
                if (!WaitHelper.WaitFor(() => !viewer.PendingSentFrames.Any(), TimeSpan.FromSeconds(30)))
                {
                    _logger.LogWarning("Timed out while waiting for first frame receipt.");
                    _appState.Viewers.TryRemove(viewer.ViewerConnectionID, out _);
                    viewer.Dispose();
                    return;
                }

                await viewer.SendDesktopStream(GetDesktopStream(screenCastRequest, viewer), screenCastRequest.StreamId);
                await viewer.SendStreamReady();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting screen casting.");
            }
        }

        private async Task CastScreen(ScreenCastRequest screenCastRequest, IViewer viewer, int sequence)
        {
            try
            {
                while (!viewer.DisconnectRequested && viewer.IsConnected)
                {
                    try
                    {
                        if (viewer.IsStalled)
                        {
                            // Viewer isn't responding.  Abort sending.
                            _logger.LogWarning("Viewer stalled.  Ending send loop.");
                            break;
                        }

                        viewer.CalculateFps();

                        viewer.ApplyAutoQuality();

                        var result = viewer.Capturer.GetNextFrame();

                        // Try to restart on a new thread if capturing failed.
                        if (!result.IsSuccess || result.Value is null)
                        {
                            _ = Task.Run(() => CastScreen(screenCastRequest, viewer, sequence));
                            return;
                        }

                        var diffArea = viewer.Capturer.GetFrameDiffArea();

                        if (diffArea.IsEmpty)
                        {
                            continue;
                        }

                        viewer.Capturer.CaptureFullscreen = false;

                        using var croppedFrame = _imageHelper.CropBitmap(result.Value, diffArea);

                        var encodedImageBytes = _imageHelper.EncodeBitmap(croppedFrame, SKEncodedImageFormat.Jpeg, viewer.ImageQuality);

                        if (encodedImageBytes.Length == 0)
                        {
                            continue;
                        }

                        await viewer.SendScreenCapture(new ScreenCaptureDto()
                        {
                            ImageBytes = encodedImageBytes,
                            Top = (int)diffArea.Top,
                            Left = (int)diffArea.Left,
                            Width = (int)diffArea.Width,
                            Height = (int)diffArea.Height,
                            Sequence = sequence++
                        });

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while casting screen.");
                    }
                }

                _logger.LogInformation(
                    "Ended screen cast.  " +
                    "Requester: {viewerName}. " +
                    "Viewer ID: {viewerConnectionID}. " +
                    "Viewer WS Connected: {viewerIsConnected}.  " +
                    "Viewer Stalled: {viewerIsStalled}.  " +
                    "Viewer Disconnected Requested: {viewerDisconnectRequested}",
                    viewer.Name,
                    viewer.ViewerConnectionID,
                    viewer.IsConnected,
                    viewer.IsStalled,
                    viewer.DisconnectRequested);

                _appState.Viewers.TryRemove(viewer.ViewerConnectionID, out _);
                viewer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while casting screen.");
            }
            finally
            {
                // Close if no one is viewing.
                if (_appState.Viewers.IsEmpty && _appState.Mode == AppMode.Unattended)
                {
                    _logger.LogInformation("No more viewers.  Calling shutdown service.");
                    await _shutdownService.Shutdown();
                }
            }
        }

        private async IAsyncEnumerable<byte[]> GetDesktopStream(ScreenCastRequest screenCastRequest, IViewer viewer)
        {
            _metricsCts?.Cancel();
            _metricsCts?.Dispose();
            _metricsCts = new CancellationTokenSource();

            try
            {
                _ = Task.Run(async () => await LogMetrics(viewer, _metricsCts.Token));

                while (!viewer.DisconnectRequested && viewer.IsConnected)
                {
                    if (viewer.IsStalled)
                    {
                        // Viewer isn't responding.  Abort sending.
                        _logger.LogWarning("Viewer stalled.  Ending send loop.");
                        yield break;
                    }

                    viewer.CalculateFps();

                    viewer.ApplyAutoQuality();

                    var result = viewer.Capturer.GetNextFrame();

                    if (!result.IsSuccess || result.Value is null)
                    {
                        continue;
                    }

                    var diffArea = viewer.Capturer.GetFrameDiffArea();

                    if (diffArea.IsEmpty)
                    {
                        continue;
                    }

                    viewer.Capturer.CaptureFullscreen = false;

                    using var croppedFrame = _imageHelper.CropBitmap(result.Value, diffArea);

                    var encodedImageBytes = _imageHelper.EncodeBitmap(croppedFrame, SKEncodedImageFormat.Jpeg, viewer.ImageQuality);

                    if (encodedImageBytes.Length == 0)
                    {
                        continue;
                    }

                    var dto = new ScreenCaptureDto()
                    {
                        ImageBytes = encodedImageBytes,
                        Top = (int)diffArea.Top,
                        Left = (int)diffArea.Left,
                        Width = (int)diffArea.Width,
                        Height = (int)diffArea.Height
                    };

                    viewer.PendingSentFrames.Enqueue(new SentFrame(dto.ImageBytes.Length, _systemTime.Now));

                    foreach (var chunk in DtoChunker.ChunkDto(dto, DtoType.ScreenCapture))
                    {
                        yield return MessagePackSerializer.Serialize(chunk);
                    }
                }
                _logger.LogInformation(
                    "Ended desktop stream.  " +
                    "Requester: {viewerName}. " +
                    "Viewer ID: {viewerConnectionID}. " +
                    "Viewer WS Connected: {viewerIsConnected}.  " +
                    "Viewer Stalled: {viewerIsStalled}.  " +
                    "Viewer Disconnected Requested: {viewerDisconnectRequested}",
                    viewer.Name,
                    viewer.ViewerConnectionID,
                    viewer.IsConnected,
                    viewer.IsStalled,
                    viewer.DisconnectRequested);

                _appState.Viewers.TryRemove(viewer.ViewerConnectionID, out _);
                viewer.Dispose();
            }
            finally
            {
                Disposer.TryDisposeAll(viewer);
                // Close if no one is viewing.
                if (_appState.Viewers.IsEmpty && _appState.Mode == AppMode.Unattended)
                {
                    _logger.LogInformation("No more viewers.  Calling shutdown service.");
                    await _shutdownService.Shutdown();
                }
                _metricsCts.Cancel();
            }
        }

        private async Task LogMetrics(IViewer viewer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(3_000, cancellationToken);
                _logger.LogDebug(
                    "Current Mbps: {currentMbps}.  " +
                    "Current FPS: {currentFps}.  " +
                    "Roundtrip Latency: {roundTripLatency}.  " +
                    "Image Quality: {imageQuality}",
                    Math.Round(viewer.CurrentMbps, 2),
                    viewer.CurrentFps,
                    viewer.RoundTripLatency,
                    viewer.ImageQuality);
            }
        }
    }
}
