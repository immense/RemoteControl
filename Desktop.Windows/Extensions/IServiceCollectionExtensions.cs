using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Extensions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.Win32;
using Immense.RemoteControl.Desktop.Windows.Services;
using Immense.RemoteControl.Desktop.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static async Task AddRemoteControlClient(
            this IServiceCollection services,
            string[] args,
            string serverUri = "",
            CancellationToken cancellationToken = default)
        {
            if (OperatingSystem.IsWindows() && args.Contains("--elevate"))
            {
                RelaunchElevated();
                return;
            }


            await services.AddRemoteControlClientCore(args, AddWindowsServices, serverUri, cancellationToken);

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
            services.AddScoped<IDtoMessageHandler, DtoMessageHandler>();
            services.AddSingleton<IAppStartup, AppStartup>();
            services.AddScoped<IRemoteControlAccessService, RemoteControlAccessServiceWin>();
            services.AddSingleton<MainWindowViewModel>();

            var backgroundForm = new Form()
            {
                Visible = false,
                Opacity = 0,
                ShowIcon = false,
                ShowInTaskbar = false,
                WindowState = System.Windows.Forms.FormWindowState.Minimized
            };
            services.AddSingleton((serviceProvider) => backgroundForm);
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
