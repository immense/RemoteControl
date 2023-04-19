using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Immense.RemoteControl.Desktop.Shared.Startup;
using Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Services;

namespace Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Startup;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds Windows and cross-platform remote control services to the service collection.
    /// All methods on <see cref="IRemoteControlClientBuilder"/> must be called to register
    /// required services.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="clientConfig"></param>
    public static void AddRemoteControlWindows(
        this IServiceCollection services,
        Action<IRemoteControlClientBuilder> clientConfig)
    {
        services.AddRemoteControlXplat(clientConfig);

        services.AddSingleton<ICursorIconWatcher, CursorIconWatcherWin>();
        services.AddSingleton<IKeyboardMouseInput, KeyboardMouseInputWin>();
        services.AddSingleton<IClipboardService, ClipboardServiceWin>();
        services.AddSingleton<IAudioCapturer, AudioCapturerWin>();
        services.AddSingleton<IChatUiService, ChatUiServiceWin>();
        services.AddSingleton<ISessionIndicator, SessionIndicatorWin>();
        services.AddSingleton<IShutdownService, ShutdownServiceWin>();
        services.AddSingleton<IWindowsUiDispatcher, WindowsUiDispatcher>();
        services.AddSingleton<IAppStartup, AppStartup>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
        services.AddTransient<IRemoteControlAccessService, RemoteControlAccessServiceWin>();
        services.AddTransient<IFileTransferService, FileTransferServiceWin>();
        services.AddTransient<IScreenCapturer, ScreenCapturerWin>();
    }
}
