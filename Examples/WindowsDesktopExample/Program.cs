using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Windows;
using Immense.RemoteControl.Desktop.Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WindowsDesktopExample;


// The service provider is returned in case it's needed.
var provider = await Startup.UseRemoteControlClient(
    args,
    config =>
    {
        config.AddBrandingProvider<BrandingProvider>();
    },
    services =>
    {
        // Add some other services here if I wanted.

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            // Add file logger, etc.
        });
    },
    "https://localhost:7024");

var shutdownService = provider.GetRequiredService<IShutdownService>();
Console.CancelKeyPress += async (s, e) =>
{
    await shutdownService.Shutdown();
};

var dispatcher = provider.GetRequiredService<IWindowsUiDispatcher>();

Console.WriteLine("Press Ctrl + C to exit.");
await Task.Delay(Timeout.InfiniteTimeSpan, dispatcher.ApplicationExitingToken);