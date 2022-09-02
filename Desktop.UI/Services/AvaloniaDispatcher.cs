using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Desktop.UI;
using Immense.RemoteControl.Desktop.Shared.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.UI.Services
{
    public interface IAvaloniaDispatcher
    {
        CancellationToken AppCancellationToken { get; }

        Task InvokeAsync(Func<Task> func);
        Task<T> InvokeAsync<T>(Func<Task<T>> func);
        void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal);
        void StartBackground();
        void StartForeground();
    }

    internal class AvaloniaDispatcher : IAvaloniaDispatcher
    {
        private static readonly CancellationTokenSource _appCts = new();
        private readonly ILogger<AvaloniaDispatcher> _logger;

        public AvaloniaDispatcher(ILogger<AvaloniaDispatcher> logger)
        {
            _logger = logger;
        }

        public CancellationToken AppCancellationToken => _appCts.Token;

        public Task InvokeAsync(Func<Task> func)
        {

            return Dispatcher.UIThread.InvokeAsync(func);
        }

        public Task<T> InvokeAsync<T>(Func<Task<T>> func)
        {
            return Dispatcher.UIThread.InvokeAsync(func);
        }

        public void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            Dispatcher.UIThread.Post(action, priority);
        }

        public void StartBackground()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                BuildAvaloniaApp().Start(MainImpl, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting backgroud app.");
                throw;
            }
        }

        public void StartForeground()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting foreground app.");
                throw;
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

        private static void MainImpl(Application app, string[] args)
        {
            app.Run(_appCts.Token);
        }
    }
}
