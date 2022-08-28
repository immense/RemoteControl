using System.Collections.Concurrent;
using System.Diagnostics;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Models;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Models.Dtos;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using Immense.RemoteControl.Desktop.Shared.Win32;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IViewer
    {
        IScreenCapturer Capturer { get; }
        double CurrentFps { get; }
        double CurrentMbps { get; }
        bool DisconnectRequested { get; set; }
        bool HasControl { get; set; }
        int ImageQuality { get; }
        bool IsConnected { get; }
        bool IsStalled { get; }
        string Name { get; set; }
        ConcurrentQueue<SentFrame> PendingSentFrames { get; }
        TimeSpan RoundTripLatency { get; }
        string ViewerConnectionID { get; set; }

        void ApplyAutoQuality();
        void CalculateFps();
        void DequeuePendingFrame();
        void Dispose();
        Task SendAudioSample(byte[] audioSample);
        Task SendClipboardText(string clipboardText);
        Task SendCtrlAltDel();
        Task SendCursorChange(CursorInfo cursorInfo);
        Task SendFile(FileUpload fileUpload, CancellationToken cancelToken, Action<double> progressUpdateCallback);
        Task SendScreenCapture(CaptureFrame screenFrame);
        Task SendScreenData(string selectedDisplay, IEnumerable<string> displayNames, int screenWidth, int screenHeight);
        Task SendScreenSize(int width, int height);
        Task SendViewerConnected();
        Task SendWindowsSessions();
    }

    public class Viewer : IDisposable, IViewer
    {
        public const int DefaultQuality = 80;

        private readonly IAudioCapturer _audioCapturer;
        private readonly IDesktopHubConnection _casterSocket;
        private readonly IClipboardService _clipboardService;
        private readonly ConcurrentQueue<DateTimeOffset> _fpsQueue = new();
        private readonly ILogger<Viewer> _logger;
        private readonly ConcurrentQueue<SentFrame> _receivedFrames = new();
        private readonly ISystemTime _systemTime;

        public Viewer(
            IDesktopHubConnection casterSocket,
            IScreenCapturer screenCapturer,
            IClipboardService clipboardService,
            IAudioCapturer audioCapturer,
            ISystemTime systemTime,
            ILogger<Viewer> logger)
        {
            Capturer = screenCapturer;
            _casterSocket = casterSocket;
            _clipboardService = clipboardService;
            _audioCapturer = audioCapturer;
            _systemTime = systemTime;
            _logger = logger;

            _clipboardService.ClipboardTextChanged += ClipboardService_ClipboardTextChanged;
            _audioCapturer.AudioSampleReady += AudioCapturer_AudioSampleReady;
        }

        public IScreenCapturer Capturer { get; }
        public double CurrentFps { get; private set; }
        public double CurrentMbps { get; private set; }
        public bool DisconnectRequested { get; set; }
        public bool HasControl { get; set; } = true;
        public int ImageQuality { get; private set; } = DefaultQuality;
        public bool IsConnected => _casterSocket.IsConnected;

        public bool IsStalled
        {
            get
            {
                return PendingSentFrames.TryPeek(out var result) && DateTimeOffset.Now - result.Timestamp > TimeSpan.FromSeconds(15);
            }
        }

        public string Name { get; set; } = string.Empty;
        public ConcurrentQueue<SentFrame> PendingSentFrames { get; } = new();
        public TimeSpan RoundTripLatency { get; private set; }

        public string ViewerConnectionID { get; set; } = string.Empty;

        public void ApplyAutoQuality()
        {
            if (ImageQuality < DefaultQuality)
            {
                ImageQuality = Math.Min(DefaultQuality, ImageQuality + 2);
            }

            // Limit FPS.
            _ = WaitHelper.WaitFor(() =>
                !PendingSentFrames.TryPeek(out var result) || DateTimeOffset.Now - result.Timestamp > TimeSpan.FromMilliseconds(50),
                TimeSpan.FromSeconds(5));

            // Delay based on roundtrip time to prevent too many frames from queuing up on slow connections.
            _ = WaitHelper.WaitFor(() => PendingSentFrames.Count < 1 / RoundTripLatency.TotalSeconds,
                TimeSpan.FromSeconds(5));

            // Wait until oldest pending frame is within the past 1 second.
            _ = WaitHelper.WaitFor(() =>
                !PendingSentFrames.TryPeek(out var result) || DateTimeOffset.Now - result.Timestamp < TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5));


            Debug.WriteLine(
                $"Current Mbps: {CurrentMbps}.  " +
                $"Current FPS: {CurrentFps}.  " +
                $"Roundtrip Latency: {RoundTripLatency}.  " +
                $"Image Quality: {ImageQuality}");
        }

        public void CalculateFps()
        {
            _fpsQueue.Enqueue(_systemTime.Now);

            while (_fpsQueue.TryPeek(out var oldestTime) &&
                _systemTime.Now - oldestTime > TimeSpan.FromSeconds(1))
            {
                _fpsQueue.TryDequeue(out _);
            }

            CurrentFps = _fpsQueue.Count;
        }

        public void DequeuePendingFrame()
        {
            if (PendingSentFrames.TryDequeue(out var frame))
            {
                RoundTripLatency = _systemTime.Now - frame.Timestamp;
                _receivedFrames.Enqueue(new SentFrame(frame.FrameSize, _systemTime.Now));
            }
            while (_receivedFrames.TryPeek(out var oldestFrame) &&
                _systemTime.Now - oldestFrame.Timestamp > TimeSpan.FromSeconds(1))
            {
                _receivedFrames.TryDequeue(out _);
            }
            CurrentMbps = (double)_receivedFrames.Sum(x => x.FrameSize) / 1024 / 1024 * 8;
        }

        public void Dispose()
        {
            DisconnectRequested = true;
            Disposer.TryDisposeAll(Capturer);
            GC.SuppressFinalize(this);
        }

        public async Task SendAudioSample(byte[] audioSample)
        {
            var dto = new AudioSampleDto(audioSample);
            await TrySendToViewer(dto, ViewerConnectionID);
        }

        public async Task SendClipboardText(string clipboardText)
        {
            var dto = new ClipboardTextDto(clipboardText);
            await TrySendToViewer(dto, ViewerConnectionID);
        }

        public async Task SendCtrlAltDel()
        {
            await _casterSocket.SendCtrlAltDelToAgent();
        }

        public async Task SendCursorChange(CursorInfo cursorInfo)
        {
            if (cursorInfo is null)
            {
                return;
            }

            var dto = new CursorChangeDto(cursorInfo.ImageBytes, cursorInfo.HotSpot.X, cursorInfo.HotSpot.Y, cursorInfo.CssOverride);
            await TrySendToViewer(dto, ViewerConnectionID);
        }
        public async Task SendFile(FileUpload fileUpload, CancellationToken cancelToken, Action<double> progressUpdateCallback)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                var fileDto = new FileDto()
                {
                    EndOfFile = false,
                    FileName = fileUpload.DisplayName,
                    MessageId = messageId,
                    StartOfFile = true
                };

                await TrySendToViewer(fileDto, ViewerConnectionID);

                using var fs = File.OpenRead(fileUpload.FilePath);
                using var br = new BinaryReader(fs);
                while (fs.Position < fs.Length)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return;
                    }

                    fileDto = new FileDto()
                    {
                        Buffer = br.ReadBytes(40_000),
                        FileName = fileUpload.DisplayName,
                        MessageId = messageId
                    };

                    await TrySendToViewer(fileDto, ViewerConnectionID);

                    progressUpdateCallback((double)fs.Position / fs.Length);
                }

                fileDto = new FileDto()
                {
                    EndOfFile = true,
                    FileName = fileUpload.DisplayName,
                    MessageId = messageId,
                    StartOfFile = false
                };

                await TrySendToViewer(fileDto, ViewerConnectionID);

                progressUpdateCallback(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending file.");
            }
        }

        public async Task SendScreenCapture(CaptureFrame screenFrame)
        {

            PendingSentFrames.Enqueue(new SentFrame(screenFrame.EncodedImageBytes.Length, _systemTime.Now));

            var left = screenFrame.Left;
            var top = screenFrame.Top;
            var width = screenFrame.Width;
            var height = screenFrame.Height;

            var chunks = screenFrame.EncodedImageBytes.Chunk(50_000).ToArray();
            var chunkCount = chunks.Length;

            for (var i = 0; i < chunkCount; i++)
            {
                var chunk = chunks[i];

                var dto = new CaptureFrameDto()
                {
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    EndOfFrame = i == chunkCount - 1,
                    Sequence = screenFrame.Sequence,
                    ImageBytes = chunk
                };

                await TrySendToViewer(dto, ViewerConnectionID);
            }
        }

        public async Task SendScreenData(
            string selectedDisplay,
            IEnumerable<string> displayNames,
            int screenWidth,
            int screenHeight)
        {
            var dto = new ScreenDataDto()
            {
                MachineName = Environment.MachineName,
                DisplayNames = displayNames,
                SelectedDisplay = selectedDisplay,
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight
            };
            await TrySendToViewer(dto, ViewerConnectionID);
        }

        public async Task SendScreenSize(int width, int height)
        {
            var dto = new ScreenSizeDto(width, height);
            await TrySendToViewer(dto, ViewerConnectionID);
        }

        public async Task SendViewerConnected()
        {
            await _casterSocket.SendViewerConnected(ViewerConnectionID);
        }

        public async Task SendWindowsSessions()
        {
            if (OperatingSystem.IsWindows())
            {
                var dto = new WindowsSessionsDto(Win32Interop.GetActiveSessions());
                await TrySendToViewer(dto, ViewerConnectionID);
            }
        }

        private async void AudioCapturer_AudioSampleReady(object? sender, byte[] sample)
        {
            await SendAudioSample(sample);
        }

        private async void ClipboardService_ClipboardTextChanged(object? sender, string clipboardText)
        {
            await SendClipboardText(clipboardText);
        }

        private async Task TrySendToViewer<T>(T dto, string viewerConnectionId)
        {
            try
            {
                foreach (var chunk in DtoChunker.ChunkDto(dto, DtoType.AudioSample))
                {
                    await _casterSocket.SendDtoToViewer(chunk, viewerConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending to viewer.");
            }
        }
    }
}
