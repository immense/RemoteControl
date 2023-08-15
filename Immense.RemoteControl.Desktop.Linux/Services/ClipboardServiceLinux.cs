using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Desktop.UI.Services;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class ClipboardServiceLinux : IClipboardService
{
    private readonly IAvaloniaDispatcher _dispatcher;
    private readonly ILogger<ClipboardServiceLinux> _logger;
    private CancellationTokenSource? _cancelTokenSource;

    public event EventHandler<string>? ClipboardTextChanged;

    public ClipboardServiceLinux(
        IAvaloniaDispatcher dispatcher,
        ILogger<ClipboardServiceLinux> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    private string ClipboardText { get; set; } = string.Empty;

    public void BeginWatching()
    {
        try
        {
            StopWatching();
        }
        finally
        {
            _cancelTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () => await WatchClipboard(_cancelTokenSource.Token));
        }
    }

    public async Task SetText(string clipboardText)
    {
        try
        {
            if (_dispatcher?.Clipboard is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                await _dispatcher.Clipboard.ClearAsync();
            }
            else
            {
                await _dispatcher.Clipboard.SetTextAsync(clipboardText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while setting text.");
        }
    }

    public void StopWatching()
    {
        _cancelTokenSource?.Cancel();
        _cancelTokenSource?.Dispose();
    }

    private async Task WatchClipboard(CancellationToken cancelToken)
    {
        while (!cancelToken.IsCancellationRequested &&
            !Environment.HasShutdownStarted)
        {
            try
            {
                if (_dispatcher?.Clipboard is null)
                {
                    continue;
                }

                var currentText = await _dispatcher.Clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(currentText) && currentText != ClipboardText)
                {
                    ClipboardText = currentText;
                    ClipboardTextChanged?.Invoke(this, ClipboardText);
                }
            }
            finally
            {
                Thread.Sleep(500);
            }
        }
    }
}