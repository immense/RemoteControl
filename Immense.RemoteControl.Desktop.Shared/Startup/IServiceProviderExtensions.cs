using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Immense.RemoteControl.Desktop.Shared.Startup;

public static class IServiceProviderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="args"></param>
    /// <param name="serverUri"></param>
    /// <returns></returns>
    internal static async Task<Result> UseRemoteControlClientXplat(
        this IServiceProvider services,
        string[] args,
        string serverUri = "")
    {
        try
        {
            await UseRemoteControlClientImpl(services, args, serverUri);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }

    }

    private static async Task UseRemoteControlClientImpl(IServiceProvider services, string[] args, string serverUri)
    {
        var rootCommand = new RootCommand(
            $"This app is using the {typeof(IServiceCollectionExtensions).Assembly.GetName().Name} library, " +
            "created by Immense Networks, which allows IT administrators to provide remote assistance on this device.\n\n" +
            "Internal arguments include the following:\n\n" +
            "--relaunch    Used to indicate that process is being relaunched from a previous session\n" +
            "              and should notify viewers when it's ready.\n" +
            "--viewers     Used with --relaunch.  Should be a comma-separated list of viewers'\n" +
            "              SignalR connection IDs.\n" +
            "--elevate     Must be called from a Windows service.  The process will relaunch itself\n" +
            "              in the console session with elevated rights.");

        var hostOption = new Option<string>(
            new[] { "-h", "--host" },
            "The hostname of the server to which to connect (e.g. https://example.com).");
        rootCommand.AddOption(hostOption);

        var modeOption = new Option<AppMode>(
            new[] { "-m", "--mode" },
            () => AppMode.Attended,
            "The remote control mode to use.  Either Attended, Unattended, or Chat.");
        rootCommand.AddOption(modeOption);


        var pipeNameOption = new Option<string>(
            new[] { "-p", "--pipe-name" },
            "When AppMode is Chat, this is the pipe name used by the named pipes server.");
        pipeNameOption.AddValidator((context) =>
        {
            if (context.GetValueForOption(modeOption) == AppMode.Chat &&
                string.IsNullOrWhiteSpace(context.GetValueOrDefault<string>()))
            {
                context.ErrorMessage = "A pipe name must be specified when AppMode is Chat.";
            }
        });
        rootCommand.AddOption(pipeNameOption);

        var sessionIdOption = new Option<string>(
           new[] { "-s", "--session-id" },
           "In Unattended mode, this unique session ID will be assigned to this connection and " +
           "shared with the server.  The connection can then be found in the DesktopHubSessionCache " +
           "using this ID.");
        rootCommand.AddOption(sessionIdOption);

        var accessKeyOption = new Option<string>(
            new[] { "-a", "--access-key" },
            "In Unattended mode, secures access to the connection using the provided key.");
        rootCommand.AddOption(accessKeyOption);

        var requesterNameOption = new Option<string>(
            new[] { "-r", "--requester-name" },
               "The name of the technician requesting to connect.");
        rootCommand.AddOption(requesterNameOption);

        var organizationNameOption = new Option<string>(
            new[] { "-o", "--org-name" },
            "The organization name of the technician requesting to connect.");
        rootCommand.AddOption(organizationNameOption);
        
        rootCommand.SetHandler(
            (
                string host,
                AppMode mode,
                string pipeName,
                string sessionId,
                string accessKey,
                string requesterName,
                string organizationName) =>
            {

                if (string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(serverUri))
                {
                    host = serverUri;
                }

                var appState = services.GetRequiredService<IAppState>();
                appState.Configure(
                    host,
                    mode,
                    sessionId,
                    accessKey,
                    requesterName,
                    organizationName,
                    pipeName);
            },
            hostOption,
            modeOption,
            pipeNameOption,
            sessionIdOption,
            accessKeyOption,
            requesterNameOption,
            organizationNameOption);

        rootCommand.TreatUnmatchedTokensAsErrors = false;
        var result = await rootCommand.InvokeAsync(args);

        if (result > 0)
        {
            Environment.Exit(result);
        }

        if (args.Any(x =>
            x.StartsWith("-h") ||
            x.StartsWith("--help") ||
            x.StartsWith("-?") ||
            x.StartsWith("/?")))
        {
            Environment.Exit(0);
        }

        StaticServiceProvider.Instance = services;

        var appStartup = services.GetRequiredService<IAppStartup>();
        await appStartup.Initialize();
    }
}
