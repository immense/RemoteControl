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
using Immense.RemoteControl.Desktop.Shared.Models;
using Immense.RemoteControl.Shared.Helpers;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IScreenCaster
    {
        void BeginScreenCasting(ScreenCastRequest screenCastRequest);
    }

    public class ScreenCaster : IScreenCaster
    {
        private readonly IAppState _appState;
        private readonly ICursorIconWatcher _cursorIconWatcher;
        private readonly ISessionIndicator _sessionIndicator;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IShutdownService _shutdownService;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger<ScreenCaster> _logger;

        public ScreenCaster(
            IAppState appState,
            ICursorIconWatcher cursorIconWatcher,
            ISessionIndicator sessionIndicator,
            IServiceScopeFactory scopeFactory,
            IShutdownService shutdownService,
            IImageHelper imageHelper,
            ILogger<ScreenCaster> logger)
        {
            _appState = appState;
            _cursorIconWatcher = cursorIconWatcher;
            _sessionIndicator = sessionIndicator;
            _scopeFactory = scopeFactory;
            _shutdownService = shutdownService;
            _imageHelper = imageHelper;
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
                using var scope = _scopeFactory.CreateScope();
                var viewer = scope.ServiceProvider.GetRequiredService<IViewer>();
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
                    await viewer.SendScreenCapture(new CaptureFrame()
                    {
                        EncodedImageBytes = _imageHelper.EncodeBitmap(result.Value, SKEncodedImageFormat.Jpeg, viewer.ImageQuality),
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

                _ = Task.Run(() => CastScreen(screenCastRequest, viewer, 0));
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

                        await SendFrame(encodedImageBytes, diffArea, sequence++, viewer);

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

        private static async Task SendFrame(byte[] encodedImageBytes, SKRect diffArea, long sequence, IViewer viewer)
        {
            if (encodedImageBytes.Length == 0)
            {
                return;
            }

            await viewer.SendScreenCapture(new CaptureFrame()
            {
                EncodedImageBytes = encodedImageBytes,
                Top = (int)diffArea.Top,
                Left = (int)diffArea.Left,
                Width = (int)diffArea.Width,
                Height = (int)diffArea.Height,
                Sequence = sequence
            });
        }
    }
}
