using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Desktop.Windows.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindowsDesktopExample;


var services = new ServiceCollection();
services.AddRemoteControlWindows(config =>
{
    config.AddBrandingProvider<BrandingProvider>();
});

services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    // Add file logger, etc.
});
// Add other services.

var provider = services.BuildServiceProvider();

var result = await provider.UseRemoteControlClientWindows(args, serverUri: "https://localhost:7024");
if (!result.IsSuccess)
{
    Console.WriteLine($"Remote control failed with message: {result.Error}");
}

var shutdownService = provider.GetRequiredService<IShutdownService>();
Console.CancelKeyPress += async (s, e) =>
{
    await shutdownService.Shutdown();
};

var dispatcher = provider.GetRequiredService<IWindowsUiDispatcher>();

Console.WriteLine("Press Ctrl + C to exit.");
try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, dispatcher.ApplicationExitingToken);
}
catch (TaskCanceledException)
{
    // Ok.
}