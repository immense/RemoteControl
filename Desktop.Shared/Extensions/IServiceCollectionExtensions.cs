using CommunityToolkit.Mvvm.Messaging;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Shared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Extensions
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// For internal use only.  I'd normally make this internal instead of public, 
        /// but for some reason, <see cref="InternalsVisibleToAttribute"/> in this
        /// project's AssemblyInfo isn't working.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="args"></param>
        /// <param name="clientConfig"></param>
        /// <param name="platformServicesConfig"></param>
        /// <param name="serverUri"></param>
        /// <returns></returns>
        public static async Task<IServiceProvider> BuildRemoteControlServiceProvider(
            this IServiceCollection services,
            string[] args,
            Action<IRemoteControlClientBuilder> clientConfig,
            Action<IServiceCollection> platformServicesConfig,
            Func<IServiceProvider, Task>? startupConfig = null,
            string serverUri = "")
        {
            var builder = new RemoteControlClientBuilder(services);
            clientConfig.Invoke(builder);
            builder.Validate();


            var rootCommand = new RootCommand(
                $"This app is using the {typeof(IServiceCollectionExtensions).Assembly.GetName().Name} library, " +
                "which allows IT administrators to provide remote assistance on this device.\n\n" +
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
                    host,
                    mode,
                    pipeName,
                    sessionId,
                    accessKey,
                    requesterName,
                    organizationName) =>
                {
                 
                    if (string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(serverUri))
                    {
                        host = serverUri;
                    }

                    services.AddSingleton<IAppState>(s =>
                    {
                        var messenger = s.GetRequiredService<IMessenger>();
                        var logger = s.GetRequiredService<ILogger<AppState>>();
                        return new AppState(messenger, logger)
                        {
                            Host = host,
                            Mode = mode,
                            SessionId = sessionId,
                            AccessKey = accessKey,
                            RequesterName = requesterName,
                            OrganizationName = organizationName,
                            PipeName = pipeName
                        };
                    });

                    AddServices(services, platformServicesConfig);
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

            var provider = services.BuildServiceProvider();
            StaticServiceProvider.Instance = provider;

            if (startupConfig is not null)
            {
                await startupConfig.Invoke(provider);
            }

            var appStartup = provider.GetRequiredService<IAppStartup>();
            await appStartup.Initialize();
            return provider;
        }

        private static void AddServices(IServiceCollection services, Action<IServiceCollection> platformServicesConfig)
        {
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
            services.AddTransient<IHubConnectionBuilder>(s => new HubConnectionBuilder());
            platformServicesConfig.Invoke(services);
        }
    }
}
