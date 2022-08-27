using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Win32;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.Windows
{
    public static class Startup
    {
        public static async Task<int> Run(
            Action<Exception> onError,
            string serverUri = "",
            CancellationToken cancellationToken = default)
        {

            try
            {
                var rootCommand = new RootCommand("Control your remote computers through SignalR.");

                var hostOption = new Option<string?>(
                    new[] { "-h", "--host" },
                    "The hostname of the server to which to connect (e.g. https://example.com).");
                rootCommand.AddOption(hostOption);

                var modeOption = new Option<AppMode>(
                    new[] { "-m", "--mode" },
                    () => AppMode.Attended,
                    "The remote control mode to use.  Either Attended, Unattended, or Chat.");
                rootCommand.AddOption(modeOption);

                var elevateOption = new Option<bool?>(
                    new[] { "-e", "--elevate"}, 
                    "Attempt to relaunch the process with elevated privileges.");
                rootCommand.AddOption(elevateOption);

                var requesterIdOption = new Option<string?>(
                   new[] { "-r", "--requester" },
                   "Attempt to relaunch the process with elevated privileges.");
                rootCommand.AddOption(requesterIdOption);

                var serviceIdOption = new Option<string?>(
                    new[] { "-s", "--service-id" },
                    "The SignalR connection ID of the service process that launched this process.");
                rootCommand.AddOption(serviceIdOption);

                var deviceIdOption = new Option<string?>(
                    new[] { "-d", "--device-id" },
                    "The unique ID (e.g. Entity PK) of this device.");
                rootCommand.AddOption(deviceIdOption);

                var organizationIdOption = new Option<string?>(
                    new[] { "-o", "--org-id" },
                    "The organization ID (e.g. Entity PK) of the technician requesting to connect.");
                rootCommand.AddOption(organizationIdOption);

                var organizationNameOption = new Option<string?>(
                    new[] { "-n", "--org-name" },
                    "The organization name of the technician requesting to connect.");
                rootCommand.AddOption(organizationNameOption);

                rootCommand.SetHandler(
                    async (
                        string? host, 
                        bool? elevate,
                        AppMode mode,
                        string? requesterId, 
                        string? serviceId, 
                        string? deviceId, 
                        string? organizationId, 
                        string? organizationName) =>
                    {
                        if (elevate == true)
                        {
                            RelaunchElevated();
                            return;
                        }
                    },
                    hostOption,
                    elevateOption,
                    modeOption,
                    requesterIdOption,
                    serviceIdOption,
                    deviceIdOption,
                    organizationIdOption,
                    organizationNameOption);

                return 0;
            }
            catch (Exception ex)
            {
                onError(ex);
                return 1;
            }
        }

        private static void RelaunchElevated()
        {
            var commandLine = Win32Interop.GetCommandLine().Replace(" --elevate", "").Replace(" -e", "");

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
}
