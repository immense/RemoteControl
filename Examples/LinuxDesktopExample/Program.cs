using Immense.RemoteControl.Desktop.Linux.Startup;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.Services;
using LinuxDesktopExample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddRemoteControlLinux(config =>
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

var result = await provider.UseRemoteControlLinux(args, serverUri: "https://localhost:7024");
if (!result.IsSuccess)
{
    Console.WriteLine($"Remote control failed with message: {result.Reason}");
}

var shutdownService = provider.GetRequiredService<IShutdownService>();
Console.CancelKeyPress += async (s, e) =>
{
    await shutdownService.Shutdown();
};

var dispatcher = provider.GetRequiredService<IAvaloniaDispatcher>();

Console.WriteLine("Press Ctrl + C to exit.");
try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, dispatcher.AppCancellationToken);
}
catch (TaskCanceledException)
{
    // Ok.
}