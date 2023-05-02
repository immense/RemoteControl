using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Models.Dtos;
using MessagePack;
using Immense.RemoteControl.Shared.Services;

namespace Immense.RemoteControl.Desktop.Shared.Services;

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
                    Height = screenBounds.Height,
                    IsLastChunk = true,
                    InstanceId = Guid.NewGuid()
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

            await viewer.SendDesktopStream(GetDesktopStream(viewer), screenCastRequest.StreamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting screen casting.");
        }
    }

    private async IAsyncEnumerable<byte[]> GetDesktopStream(IViewer viewer)
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

                viewer.PendingSentFrames.Enqueue(new SentFrame(encodedImageBytes.Length, _systemTime.Now));

                var instanceId = Guid.NewGuid();
                var chunks = encodedImageBytes.Chunk(50_000).ToArray();
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunk = chunks[i];
                    var dto = new ScreenCaptureDto()
                    {
                        ImageBytes = chunk,
                        Top = (int)diffArea.Top,
                        Left = (int)diffArea.Left,
                        Width = (int)diffArea.Width,
                        Height = (int)diffArea.Height,
                        InstanceId = instanceId,
                        IsLastChunk = i == chunks.Length - 1
                    };
                    yield return MessagePackSerializer.Serialize(dto);
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
