using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.Windows;
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
    null,
    "https://localhost:7024");

var shutdownService = provider.GetRequiredService<IShutdownService>();
Console.CancelKeyPress += async (s, e) =>
{
    await shutdownService.Shutdown();
};

var dispatcher = provider.GetRequiredService<IAvaloniaDispatcher>();

Console.WriteLine("Press Ctrl + C to exit.");
await Task.Delay(Timeout.InfiniteTimeSpan, dispatcher.AppCancellationToken);