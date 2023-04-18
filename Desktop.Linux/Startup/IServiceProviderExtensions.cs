using Immense.RemoteControl.Desktop.Shared.Startup;
using Immense.RemoteControl.Shared;

namespace Immense.RemoteControl.Desktop.Linux.Startup;
public static class IServiceProviderExtensions
{
    /// <summary>
    /// Runs the remote control startup pipeline.
    /// </summary>
    /// <param name="args">The command line args that were originally passed into the process.</param>
    /// <param name="serverUri">Optional.  This will be used as a fallback URI if --host parameter isn't specified.</param>
    /// <returns>The configured <see cref="IServiceProvider"/>, in case it's needed by the parent app.</returns>
    public static async Task<Result> UseRemoteControlLinux(
        this IServiceProvider serviceProvider,
        string[] args,
        string serverUri = "")
    {
        return await serviceProvider.UseRemoteControlClientXplat(args, serverUri);
    }

}
