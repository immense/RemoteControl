using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Examples.WindowsDesktopExample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Desktop.Shared.Startup;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Startup;
using Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Services;
using System.Windows.Forms;
using Immense.RemoteControl.Desktop.UI.Services;
using Avalonia;
using Immense.RemoteControl.Desktop.UI;

namespace Immense.RemoteControl.Examples.WindowsDesktopExample;

public class Program
{
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static async Task Main(string[] args)
    {
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

        var result = await provider.UseRemoteControlClient(
            args,
            "The remote control client for Remotely.",
            serverUri: "https://localhost:7024");

        if (!result.IsSuccess)
        {
            Console.WriteLine($"Remote control failed with message: {result.Reason}");
        }

        var shutdownService = provider.GetRequiredService<IShutdownService>();
        Console.CancelKeyPress += async (s, e) =>
        {
            await shutdownService.Shutdown();
        };

        var appState = provider.GetRequiredService<IAppState>();
        Console.WriteLine("Unattended session ready at (copied to clipboard): ");
        var url = $"https://localhost:7024/RemoteControl/Viewer?mode=Unattended&sessionId={appState.SessionId}&accessKey={appState.AccessKey}";
        Console.WriteLine(url);
        var thread = new Thread(() =>
        {
            Clipboard.SetDataObject(url);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Console.WriteLine("Press Ctrl + C to exit.");
        var dispatcher = provider.GetRequiredService<IUiDispatcher>();
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, dispatcher.ApplicationExitingToken);
        }
        catch (TaskCanceledException)
        {
            // Ok.
        }
    }
}