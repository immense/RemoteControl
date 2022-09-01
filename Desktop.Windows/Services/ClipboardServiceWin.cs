using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Win32;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public class ClipboardServiceWin : IClipboardService
    {
        private readonly IWpfDispatcher _dispatcher;
        private readonly ILogger<ClipboardServiceWin> _logger;
        private CancellationTokenSource? _cancelTokenSource;
        private string _clipboardText = string.Empty;

        public ClipboardServiceWin(IWpfDispatcher dispatcher, ILogger<ClipboardServiceWin> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public event EventHandler<string>? ClipboardTextChanged;

        public void BeginWatching()
        {
            _dispatcher.Invoke(() =>
            {
                _dispatcher.CurrentApp.Exit -= App_Exit;
                _dispatcher.CurrentApp.Exit += App_Exit;
            });

            StopWatching();

            _cancelTokenSource = new CancellationTokenSource();


            WatchClipboard(_cancelTokenSource.Token);
        }

        public Task SetText(string clipboardText)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(clipboardText))
                    {
                        Clipboard.Clear();
                    }
                    else
                    {
                        Clipboard.SetText(clipboardText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while setting clipboard text.");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return Task.CompletedTask;
        }

        public void StopWatching()
        {
            try
            {
                _cancelTokenSource?.Cancel();
                _cancelTokenSource?.Dispose();
            }
            catch { }
        }

        private void App_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            _cancelTokenSource?.Cancel();
        }

        private void WatchClipboard(CancellationToken cancelToken)
        {
            var thread = new Thread(() =>
            {

                while (!cancelToken.IsCancellationRequested)
                {

                    try
                    {
                        Win32Interop.SwitchToInputDesktop();

                        if (Clipboard.ContainsText() && Clipboard.GetText() != _clipboardText)
                        {
                            _clipboardText = Clipboard.GetText();
                            ClipboardTextChanged?.Invoke(this, _clipboardText);
                        }
                    }
                    catch { }
                    Thread.Sleep(500);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
