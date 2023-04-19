﻿using CommunityToolkit.Mvvm.Messaging;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Shared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Immense.RemoteControl.Desktop.Shared.Startup;

public static class IServiceCollectionExtensions
{
    internal static void AddRemoteControlXplat(
        this IServiceCollection services,
        Action<IRemoteControlClientBuilder> clientConfig)
    {
        var builder = new RemoteControlClientBuilder(services);
        clientConfig.Invoke(builder);
        builder.Validate();

        services.AddLogging(builder =>
        {
            builder.AddConsole().AddDebug();
        });

        services.AddSingleton<ISystemTime, SystemTime>();
        services.AddSingleton<IScreenCaster, ScreenCaster>();
        services.AddSingleton<IDesktopHubConnection, DesktopHubConnection>();
        services.AddSingleton<IIdleTimer, IdleTimer>();
        services.AddSingleton<IImageHelper, ImageHelper>();
        services.AddSingleton<IChatHostService, ChatHostService>();
        services.AddSingleton<IMessenger>(s => WeakReferenceMessenger.Default);
        services.AddSingleton<IEnvironmentHelper, EnvironmentHelper>();
        services.AddScoped<IDtoMessageHandler, DtoMessageHandler>();
        services.AddTransient<IViewer, Viewer>();
        services.AddSingleton<IAppState, AppState>();
        services.AddTransient<IHubConnectionBuilder>(s => new HubConnectionBuilder());
    }
}