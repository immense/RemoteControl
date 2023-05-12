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
using Microsoft.IO;

namespace Immense.RemoteControl.Desktop.Shared.Services;

public interface IScreenCaster : IDisposable
{
    Task BeginScreenCasting(ScreenCastRequest screenCastRequest);
}

internal class ScreenCaster : IScreenCaster
{
    private readonly RecyclableMemoryStreamManager _recycleStreams = new();
    private readonly IAppState _appState;
    private readonly ICursorIconWatcher _cursorIconWatcher;
    private readonly IImageHelper _imageHelper;
    private readonly ILogger<ScreenCaster> _logger;
    private readonly CancellationTokenSource _metricsCts = new();
    private readonly ISessionIndicator _sessionIndicator;
    private readonly IShutdownService _shutdownService;
    private readonly ISystemTime _systemTime;
    private readonly IViewerFactory _viewerFactory;
    private bool _disposedValue;

    public ScreenCaster(
        IAppState appState,
        IViewerFactory viewerFactory,
        ICursorIconWatcher cursorIconWatcher,
        ISessionIndicator sessionIndicator,
        IShutdownService shutdownService,
        IImageHelper imageHelper,
        ISystemTime systemTime,
        ILogger<ScreenCaster> logger)
    {
        _appState = appState;
        _cursorIconWatcher = cursorIconWatcher;
        _sessionIndicator = sessionIndicator;
        _shutdownService = shutdownService;
        _imageHelper = imageHelper;
        _systemTime = systemTime;
        _viewerFactory = viewerFactory;
        _logger = logger;
    }

    public async Task BeginScreenCasting(ScreenCastRequest screenCastRequest)
    {
        await Task.Run(() => BeginScreenCastingImpl(screenCastRequest));
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _metricsCts.Cancel();
                _metricsCts.Dispose();
            }

            _disposedValue = true;
        }
    }

    private async Task BeginScreenCastingImpl(ScreenCastRequest screenCastRequest)
    {
        using var viewer = _viewerFactory.CreateViewer(screenCastRequest.RequesterName, screenCastRequest.ViewerId);

        try
        {
            viewer.Name = screenCastRequest.RequesterName;
            viewer.ViewerConnectionId = screenCastRequest.ViewerId;

            var screenBounds = viewer.Capturer.CurrentScreenBounds;

            _logger.LogInformation(
                "Starting screen cast.  Requester: {viewerName}. " +
                "Viewer ID: {viewerViewerConnectionID}.  App Mode: {mode}",
                viewer.Name,
                viewer.ViewerConnectionId,
                _appState.Mode);

            _appState.Viewers.AddOrUpdate(viewer.ViewerConnectionId, viewer, (id, v) => viewer);

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

            _ = Task.Run(() => LogMetrics(viewer, _metricsCts.Token));
            using var sessionEndSignal = new SemaphoreSlim(0, 1);
            await viewer.SendDesktopStream(GetDesktopStream(viewer, sessionEndSignal), screenCastRequest.StreamId);
            if (!await sessionEndSignal.WaitAsync(TimeSpan.FromHours(8)))
            {
                _logger.LogWarning("Timed out while waiting for session to end.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting screen casting.");
        }
        finally
        {
            _logger.LogInformation(
                "Ended desktop stream.  " +
                "Requester: {viewerName}. " +
                "Viewer ID: {viewerConnectionID}. " +
                "Viewer Responsive: {isResponsive}.  " +
                "Viewer Disconnected Requested: {viewerDisconnectRequested}",
                viewer.Name,
                viewer.ViewerConnectionId,
                viewer.IsResponsive,
                viewer.DisconnectRequested);

            _appState.Viewers.TryRemove(viewer.ViewerConnectionId, out _);
            Disposer.TryDisposeAll(viewer);

            // Close if no one is viewing.
            if (_appState.Viewers.IsEmpty && _appState.Mode == AppMode.Unattended)
            {
                _logger.LogInformation("No more viewers.  Calling shutdown service.");
                await _shutdownService.Shutdown();
            }
        }
    }

    private async IAsyncEnumerable<byte[]> GetDesktopStream(IViewer viewer, SemaphoreSlim sessionEndedSignal)
    {
        await Task.Yield();

        try
        {
            while (!viewer.DisconnectRequested && viewer.IsResponsive)
            {
                viewer.IncrementFpsCount();

                await viewer.ApplyAutoQuality();

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

                using var frameStream = _recycleStreams.GetStream();
                using var writer = new BinaryWriter(frameStream);
                writer.Write(encodedImageBytes.Length);
                writer.Write(diffArea.Left);
                writer.Write(diffArea.Top);
                writer.Write(diffArea.Width);
                writer.Write(diffArea.Height);
                writer.Write(encodedImageBytes);

                frameStream.Seek(0, SeekOrigin.Begin);

                foreach (var chunk in frameStream.ToArray().Chunk(50_000))
                {
                    yield return chunk;
                }

                viewer.AppendSentFrame(new SentFrame(encodedImageBytes.Length, _systemTime.Now));
            }

        }
        finally
        {
            sessionEndedSignal.Release();
        }

    }

    private async Task LogMetrics(IViewer viewer, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await viewer.CalculateMetrics();

            var metrics = new SessionMetricsDto(
                Math.Round(viewer.CurrentMbps, 2),
                viewer.CurrentFps,
                viewer.RoundTripLatency.TotalMilliseconds,
                viewer.Capturer.IsGpuAccelerated);

            _logger.LogDebug(
                "Current Mbps: {currentMbps}.  " +
                "Current FPS: {currentFps}.  " +
                "Roundtrip Latency: {roundTripLatency}ms.  " +
                "Image Quality: {imageQuality}",
                metrics.Mbps,
                metrics.Fps,
                metrics.RoundTripLatency,
                viewer.ImageQuality);


            await viewer.SendSessionMetrics(metrics);
        }
    }
}
