using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Extensions;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Windows.Services;
using Immense.RemoteControl.Desktop.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows
{
    public static class Startup
    {
        /// <summary>
        /// Adds remote control services to a console or WPF app.  This will also apply command line
        /// argument parsing.
        /// </summary>
        /// <param name="args">The command line args that were originally passed into the process.</param>
        /// <param name="clientConfig">Provides methods for adding required service implementations.</param>
        /// <param name="serviceConfig">Allows registering additional services needed by the parent app.</param>
        /// <param name="serverUri">Optional.  This will be used as a fallback URI if --host parameter isn't specified.</param>
        /// <returns>The configured <see cref="IServiceProvider"/>, in case it's needed by the parent app.</returns>
        public static async Task<IServiceProvider> UseRemoteControlClient(
            string[] args,
            Action<IRemoteControlClientBuilder> clientConfig,
            Action<IServiceCollection>? serviceConfig = null,
            string serverUri = "")
        {
            var services = new ServiceCollection();

            if (OperatingSystem.IsWindows() && args.Contains("--elevate"))
            {
                RelaunchElevated();
                return services.BuildServiceProvider();
            }

            serviceConfig?.Invoke(services);

            return await services.BuildRemoteControlServiceProvider(args, clientConfig, AddWindowsServices, serverUri);
        }


        private static void AddWindowsServices(IServiceCollection services)
        {
            services.AddSingleton<ICursorIconWatcher, CursorIconWatcherWin>();
            services.AddSingleton<IKeyboardMouseInput, KeyboardMouseInputWin>();
            services.AddSingleton<IClipboardService, ClipboardServiceWin>();
            services.AddSingleton<IAudioCapturer, AudioCapturerWin>();
            services.AddSingleton<IChatUiService, ChatUiServiceWin>();
            services.AddTransient<IScreenCapturer, ScreenCapturerWin>();
            services.AddScoped<IFileTransferService, FileTransferServiceWin>();
            services.AddSingleton<ISessionIndicator, SessionIndicatorWin>();
            services.AddSingleton<IShutdownService, ShutdownServiceWin>();
            services.AddScoped<IRemoteControlAccessService, RemoteControlAccessServiceWin>();
            services.AddSingleton<IWindowsUiDispatcher, WindowsUiDispatcher>();
            services.AddSingleton<IAppStartup, AppStartup>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
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
}
