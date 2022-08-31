using Immense.RemoteControl.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public interface IWpfDispatcher
    {
        CancellationToken ApplicationExitingToken { get; }

        void Invoke(Action action);
        T? Invoke<T>(Func<T> func);
        Task InvokeAsync(Action action);
        Task<Result<T>> InvokeAsync<T>(Func<T> func);
        void StartWpfThread();
    }

    internal class WpfDispatcher : IWpfDispatcher
    {
        private readonly CancellationTokenSource _appExitCts = new();
        private Thread? _wpfThread;

        public CancellationToken ApplicationExitingToken => _appExitCts.Token;
        public void Invoke(Action action)
        {
            WpfApp.Current?.Dispatcher.Invoke(action);
        }

        public T? Invoke<T>(Func<T> func)
        {
            if (WpfApp.Current is null)
            {
                return default;
            }
            return WpfApp.Current.Dispatcher.Invoke(func);
        }

        public async Task InvokeAsync(Action action)
        {
            if (WpfApp.Current is null)
            {
                return;
            }

            await WpfApp.Current.Dispatcher.InvokeAsync(action);
        }

        public async Task<Result<T>> InvokeAsync<T>(Func<T> func)
        {
            if (WpfApp.Current is null)
            {
                return Result.Fail<T>("Application.Current is null.");
            }

            var result = await WpfApp.Current.Dispatcher.InvokeAsync(func);
            return Result.Ok(result);
        }

        public void StartWpfThread()
        {
            if (WpfApp.Current is not null)
            {
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    WpfApp.Current.Exit += (s, e) =>
                    {
                        _appExitCts.Cancel();
                    };

                });

                return;
            }

            _wpfThread = new Thread(() =>
            {
                var wpfApp = new WpfApp();
                var rd = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Resources/Styles.xaml")
                };
                wpfApp.Resources.MergedDictionaries.Add(rd);
                wpfApp.Exit += (s, e) =>
                {
                    _appExitCts.Cancel();
                };
                wpfApp.Run();
            });

            _wpfThread.SetApartmentState(ApartmentState.STA);
            _wpfThread.Start();
        }
    }
}
