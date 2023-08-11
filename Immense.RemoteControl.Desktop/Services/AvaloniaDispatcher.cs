using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Services;

public interface IAvaloniaDispatcher
{
    CancellationToken AppCancellationToken { get; }
    IClipboard? Clipboard { get; }
    Application? CurrentApp { get; }

    Window? MainWindow { get; }

    void Invoke(Action action);
    Task InvokeAsync(Action action, DispatcherPriority priority = default);
    Task InvokeAsync(Func<Task> func, DispatcherPriority priority = default);
    Task<T> InvokeAsync<T>(Func<Task<T>> func, DispatcherPriority priority = default);
    void Post(Action action, DispatcherPriority priority = default);
    void Shutdown();
    void StartAttended();

    void StartUnattended();
}

internal class AvaloniaDispatcher : IAvaloniaDispatcher
{
    private static readonly CancellationTokenSource _appCts = new();
    private static Application? _currentApp;
    private readonly ILogger<AvaloniaDispatcher> _logger;
    private AppBuilder? _appBuilder;
    public AvaloniaDispatcher(ILogger<AvaloniaDispatcher> logger)
    {
        _logger = logger;
    }

    public CancellationToken AppCancellationToken => _appCts.Token;

    public IClipboard? Clipboard
    {
        get
        {
            if (CurrentApp?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
            {
                return desktopApp.MainWindow?.Clipboard;
            }

            if (CurrentApp?.ApplicationLifetime is ISingleViewApplicationLifetime svApp)
            {
                return TopLevel.GetTopLevel(svApp.MainView)?.Clipboard;
            }
            return null;
        }
    }

    public Application? CurrentApp => _currentApp ?? _appBuilder?.Instance;

    public Window? MainWindow
    {
        get
        {
            if (CurrentApp?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            {
                return app.MainWindow;
            }

            return null;
        }
    }
    public void Invoke(Action action)
    {
        Dispatcher.UIThread.Invoke(action);
    }

    public Task InvokeAsync(Func<Task> func, DispatcherPriority priority = default)
    {

        return Dispatcher.UIThread.InvokeAsync(func, priority);
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> func, DispatcherPriority priority = default)
    {
        return Dispatcher.UIThread.InvokeAsync(func, priority);
    }

    public async Task InvokeAsync(Action action, DispatcherPriority priority = default)
    {
        await Dispatcher.UIThread.InvokeAsync(action, priority);
    }

    public void Post(Action action, DispatcherPriority priority = default)
    {
        Dispatcher.UIThread.Post(action, priority);
    }

    public void Shutdown()
    {
        _appCts.Cancel();
        if (_currentApp?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (!lifetime.TryShutdown())
            {
                Environment.Exit(0);
            }
        }
    }

    public void StartAttended()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            _appBuilder = BuildAvaloniaApp();
            _appBuilder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting foreground app.");
            throw;
        }
    }

    public void StartUnattended()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            var argString = string.Join(", ", args);
            _logger.LogInformation("Starting dispatcher in unattended mode with args: [{args}].", argString);
            _appBuilder = BuildAvaloniaApp();
            _appBuilder.Start(MainImpl, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting background app.");
            throw;
        }
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void MainImpl(Application app, string[] args)
    {
        _currentApp = app;
        app.Run(_appCts.Token);
    }
}
