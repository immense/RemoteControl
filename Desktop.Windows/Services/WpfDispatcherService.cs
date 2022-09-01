using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public interface IWpfDispatcher
    {
        CancellationToken ApplicationExitingToken { get; }
        Application CurrentApp { get; }
        void Invoke(Action action);
        T? Invoke<T>(Func<T> func);
        Task InvokeAsync(Action action);
        Task<Result<T>> InvokeAsync<T>(Func<T> func);
        Task<bool> StartWpfThread();
    }

    internal class WpfDispatcher : IWpfDispatcher
    {
        private readonly CancellationTokenSource _appExitCts = new();
        private readonly ManualResetEvent _initSignal = new(false);
        private Application? _wpfApp;
        private Thread? _wpfThread;

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


        public void Invoke(Action action)
        {
            _initSignal.WaitOne();
            _wpfApp?.Dispatcher.Invoke(action);
        }

        public T? Invoke<T>(Func<T> func)
        {
            _initSignal.WaitOne();
            if (_wpfApp is null)
            {
                return default;
            }
            return _wpfApp.Dispatcher.Invoke(func);
        }

        public async Task InvokeAsync(Action action)
        {
            _initSignal.WaitOne();
            if (_wpfApp is null)
            {
                return;
            }

            await _wpfApp.Dispatcher.InvokeAsync(action);
        }

        public async Task<Result<T>> InvokeAsync<T>(Func<T> func)
        {
            _initSignal.WaitOne();
            if (_wpfApp is null)
            {
                return Result.Fail<T>("WPF app is null.");
            }

            var result = await _wpfApp.Dispatcher.InvokeAsync(func);
            return Result.Ok(result);
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
    }
}
