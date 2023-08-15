using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Immense.RemoteControl.Desktop.UI.Startup;

internal static class IServiceCollectionExtensions
{
    internal static void AddRemoteControlUi(
       this IServiceCollection services)
    {
        services.AddSingleton<IAvaloniaDispatcher, AvaloniaDispatcher>();
        services.AddSingleton<IChatUiService, ChatUiService>();
        services.AddScoped<IRemoteControlAccessService, RemoteControlAccessService>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
        services.AddSingleton<IMainViewViewModel, MainViewViewModel>();
        services.AddSingleton<ISessionIndicatorWindowViewModel, SessionIndicatorWindowViewModel>();
        services.AddTransient<IMessageBoxViewModel, MessageBoxViewModel>();
    }
}
