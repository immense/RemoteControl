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
            services.AddSingleton<IWpfDispatcher, WpfDispatcher>();
            services.AddSingleton<IAppStartup, AppStartup>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
            services.AddSingleton((serviceProvider) => GetBackgroundForm());
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

        private static Form GetBackgroundForm()
        {
            return new Form()
            {
                Visible = false,
                Opacity = 0,
                ShowIcon = false,
                ShowInTaskbar = false,
                WindowState = System.Windows.Forms.FormWindowState.Minimized
            };
        }
    }
}
