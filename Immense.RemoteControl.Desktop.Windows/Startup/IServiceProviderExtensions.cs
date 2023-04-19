using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Desktop.Shared.Startup;

namespace Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Startup;

public static class IServiceProviderExtensions
{
    /// <summary>
    /// Runs the remote control startup pipeline.
    /// </summary>
    /// <param name="args">The command line args that were originally passed into the process.</param>
    /// <param name="serverUri">Optional.  This will be used as a fallback URI if --host parameter isn't specified.</param>
    public static async Task<Result> UseRemoteControlClientWindows(
        this IServiceProvider serviceProvider,
        string[] args,
        string serverUri = "")
    {
        if (OperatingSystem.IsWindows() && args.Contains("--elevate"))
        {
            RelaunchElevated();
            return Result.Ok();
        }

        return await serviceProvider.UseRemoteControlClientXplat(args, serverUri);
    }

    private static void RelaunchElevated()
    {
        var commandLine = Win32Interop.GetCommandLine().Replace(" --elevate", "");

        Console.WriteLine($"Elevating process {commandLine}.");
        var result = Win32Interop.OpenInteractiveProcess(
            commandLine,
            -1,
            false,
            "default",
            true,
            out var procInfo);
        Console.WriteLine($"Elevate result: {result}. Process ID: {procInfo.dwProcessId}.");
        Environment.Exit(0);
    }
}
