using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Extensions
{
    public static class IServiceCollectionExtensions
    {
        internal static async Task AddRemoteControlClientCore(
            this IServiceCollection services,
            string[] args,
            Action<IServiceCollection> platformServicesConfig,
            string serverUri = "",
            CancellationToken cancellationToken = default)
        {

            var rootCommand = new RootCommand(
                $"This app is using the {typeof(IServiceCollectionExtensions).Assembly.GetName().Name} library, " +
                $"which allows IT administrators to provide remote assistance on this device.");

            var hostOption = new Option<string>(
                new[] { "-h", "--host" },
                "The hostname of the server to which to connect (e.g. https://example.com).");
            if (string.IsNullOrWhiteSpace(serverUri))
            {
                hostOption.IsRequired = true;
            }
            rootCommand.AddOption(hostOption);

            var modeOption = new Option<AppMode>(
                new[] { "-m", "--mode" },
                () => AppMode.Attended,
                "The remote control mode to use.  Either Attended, Unattended, or Chat.");
            rootCommand.AddOption(modeOption);


            var pipeNameOption = new Option<string>(
                new[] { "-p, --pipe-name" },
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

            var requesterIdOption = new Option<string>(
               new[] { "-r", "--requester" },
               "Attempt to relaunch the process with elevated privileges.");
            rootCommand.AddOption(requesterIdOption);

            var serviceIdOption = new Option<string>(
                new[] { "-s", "--service-id" },
                "The SignalR connection ID of the service process that launched this process.");
            rootCommand.AddOption(serviceIdOption);

            var deviceIdOption = new Option<string>(
                new[] { "-d", "--device-id" },
                "The unique ID (e.g. Entity PK) of this device.");
            rootCommand.AddOption(deviceIdOption);

            var organizationIdOption = new Option<string>(
                new[] { "-o", "--org-id" },
                "The organization ID (e.g. Entity PK) of the technician requesting to connect.");
            rootCommand.AddOption(organizationIdOption);

            var organizationNameOption = new Option<string>(
                new[] { "-n", "--org-name" },
                "The organization name of the technician requesting to connect.");
            rootCommand.AddOption(organizationNameOption);
           
            rootCommand.SetHandler(
                (
                    host,
                    mode,
                    pipeName,
                    requesterId,
                    serviceId,
                    deviceId,
                    organizationId,
                    organizationName) =>
                {
                 
                    if (string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(serverUri))
                    {
                        host = serverUri;
                    }

                    AddServices(services, platformServicesConfig);
                    services.AddSingleton<IAppState>(s =>
                    {
                        var appState = new AppState()
                        {
                            DeviceID = deviceId,
                            Host = host,
                            Mode = mode,
                            OrganizationId = organizationId,
                            OrganizationName = organizationName,
                            RequesterConnectionId = requesterId,
                            ServiceConnectionId = serviceId
                        };
                        return appState;
                    });
                },
                hostOption,
                modeOption,
                pipeNameOption,
                requesterIdOption,
                serviceIdOption,
                deviceIdOption,
                organizationIdOption,
                organizationNameOption);

            await rootCommand.InvokeAsync(args);
        }

        private static void AddServices(IServiceCollection services, Action<IServiceCollection> platformServicesConfig)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole().AddDebug();
            });

            services.AddSingleton<IScreenCaster, ScreenCaster>();
            services.AddSingleton<IDesktopHubConnection, DesktopHubConnection>();
            services.AddSingleton<IIdleTimer, IdleTimer>();
            services.AddSingleton<IChatHostService, ChatHostService>();
            services.AddTransient<IViewer, Viewer>();
            services.AddScoped<IDtoMessageHandler, DtoMessageHandler>();
            platformServicesConfig.Invoke(services);
        }
    }
}
