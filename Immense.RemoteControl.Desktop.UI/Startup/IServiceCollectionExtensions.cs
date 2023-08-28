using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Immense.RemoteControl.Desktop.UI.Startup;

internal static class IServiceCollectionExtensions
{
    internal static void AddRemoteControlUi(
       this IServiceCollection services)
    {
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<IChatUiService, ChatUiService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ISessionIndicator, SessionIndicator>();
        services.AddSingleton<IRemoteControlAccessService, RemoteControlAccessService>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
        services.AddSingleton<IMainViewViewModel, MainViewViewModel>();
        services.AddSingleton<ISessionIndicatorWindowViewModel, SessionIndicatorWindowViewModel>();
        services.AddTransient<IMessageBoxViewModel, MessageBoxViewModel>();
        services.AddSingleton<IDialogProvider, DialogProvider>();
    }
}
