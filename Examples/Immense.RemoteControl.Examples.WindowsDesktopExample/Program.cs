using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Desktop.Shared.Startup;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.Services;
using Avalonia;
using Immense.RemoteControl.Desktop.UI;
using Immense.RemoteControl.Desktop.Windows.Startup;
using Immense.RemoteControl.Desktop.Shared.Native.Windows;
using System.Runtime.Versioning;
using System.Text;
using System.Runtime.InteropServices;

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
        Console.WriteLine($"\n{url}\n");

        var terminatedUrl = $"{url}\0";
        var urlBytes = Encoding.Unicode.GetBytes(terminatedUrl);
        var handle = Marshal.AllocHGlobal(urlBytes.Length);
        Marshal.Copy(urlBytes, 0, handle, urlBytes.Length);

        User32.OpenClipboard(nint.Zero);
        User32.EmptyClipboard();
        User32.SetClipboardData(13, handle);
        User32.CloseClipboard();
        Marshal.FreeHGlobal(handle);

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