using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Immense.RemoteControl.Desktop.Services.Linux;

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
            if (_dispatcher.CurrentApp?.Clipboard is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                await _dispatcher.CurrentApp.Clipboard.ClearAsync();
            }
            else
            {
                await _dispatcher.CurrentApp.Clipboard.SetTextAsync(clipboardText);
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
                if (_dispatcher.CurrentApp?.Clipboard is null)
                {
                    continue;
                }

                var currentText = await _dispatcher.CurrentApp.Clipboard.GetTextAsync();
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
