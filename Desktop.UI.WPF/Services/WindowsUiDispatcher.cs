using CommunityToolkit.Mvvm.Messaging;
using Immense.RemoteControl.Desktop.Shared.Messages;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using SessionSwitchReasonLocal = Immense.RemoteControl.Shared.Enums.SessionSwitchReason;

namespace Immense.RemoteControl.Desktop.UI.WPF.Services
{
    public interface IWindowsUiDispatcher
    {
        CancellationToken ApplicationExitingToken { get; }
        Application CurrentApp { get; }
        void InvokeWinForms(Action action);

        void InvokeWpf(Action action);
        T? InvokeWpf<T>(Func<T> func);
        Task InvokeWpfAsync(Action action);
        Task<Result<T>> InvokeWpfAsync<T>(Func<T> func);
        void StartWinFormsThread();
        Task<bool> StartWpfThread();
    }

    public class WindowsUiDispatcher : IWindowsUiDispatcher
    {
        private readonly CancellationTokenSource _appExitCts = new();
        private readonly ManualResetEvent _initSignal = new(false);
        private readonly IMessenger _messenger;
        private readonly ILogger<WindowsUiDispatcher> _logger;
        private Form? _backgroundForm;
        private Thread? _winformsThread;
        private Application? _wpfApp;
        private Thread? _wpfThread;

        public WindowsUiDispatcher(
            IMessenger messenger,
            ILogger<WindowsUiDispatcher> logger)
        {
            _messenger = messenger;
            _logger = logger;
        }
        public CancellationToken ApplicationExitingToken => _appExitCts.Token;

        public Application CurrentApp
        {
            get
            {
                _initSignal.WaitOne();
                if (_wpfApp is null)
                {
                    throw new Exception("WPF app hasn't been started yet.");
                }
                return _wpfApp;
            }
        }

        public void InvokeWinForms(Action action)
        {
            _backgroundForm?.Invoke(action);
        }

        public void InvokeWpf(Action action)
        {
            _initSignal.WaitOne();
            _wpfApp?.Dispatcher.Invoke(action);
        }

        public T? InvokeWpf<T>(Func<T> func)
        {
            _initSignal.WaitOne();
            if (_wpfApp is null)
            {
                return default;
            }
            return _wpfApp.Dispatcher.Invoke(func);
        }

        public async Task InvokeWpfAsync(Action action)
        {
            _initSignal.WaitOne();
            if (_wpfApp is null)
            {
                return;
            }

            await _wpfApp.Dispatcher.InvokeAsync(action);
        }

        public async Task<Result<T>> InvokeWpfAsync<T>(Func<T> func)
        {
            _initSignal.WaitOne();
            if (_wpfApp is null)
            {
                return Result.Fail<T>("WPF app is null.");
            }

            var result = await _wpfApp.Dispatcher.InvokeAsync(func);
            return Result.Ok(result);
        }

        public void StartWinFormsThread()
        {
            _backgroundForm = new Form()
            {
                Visible = false,
                Opacity = 0,
                ShowIcon = false,
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            _backgroundForm.Load += BackgroundForm_Load;
            _backgroundForm.FormClosing += BackgroundForm_FormClosing;
            _winformsThread = new Thread(() =>
            {
                System.Windows.Forms.Application.Run(_backgroundForm);
            })
            {
                IsBackground = true
            };
            _winformsThread.TrySetApartmentState(ApartmentState.STA);
            _winformsThread.Start();

            _logger.LogInformation("Background WinForms thread started.");
        }

        public async Task<bool> StartWpfThread()
        {
            try
            {
                if (Application.Current is not null)
                {
                    _wpfApp = Application.Current;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Exit += (s, e) =>
                        {
                            _appExitCts.Cancel();
                        };

                    });

                    return true;
                }

                var startedSignal = new SemaphoreSlim(0, 1);

                _wpfThread = new Thread(() =>
                {
                    _wpfApp = new Application();
                    _wpfApp.Startup += (s, e) =>
                    {
                        startedSignal.Release();
                    };
                    _wpfApp.Exit += (s, e) =>
                    {
                        _appExitCts.Cancel();
                    };
                    _wpfApp.Run();
                });

                _wpfThread.SetApartmentState(ApartmentState.STA);
                _wpfThread.Start();

                return await startedSignal.WaitAsync(5_000).ConfigureAwait(false);
            }
            finally
            {
                _initSignal.Set();
            }
        }

        private void BackgroundForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
        }

        private void BackgroundForm_Load(object? sender, EventArgs e)
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (e.Reason == SessionEndReasons.SystemShutdown)
            {
                _messenger.Send<DisconnectAllViewersMessage>();
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            _logger.LogInformation("Session changing.  Reason: {reason}", e.Reason);

            var reason = (SessionSwitchReasonLocal)(int)e.Reason;
            _messenger.Send(new NotifySessionChangedMessage(reason, Process.GetCurrentProcess().SessionId));
        }
    }
}
