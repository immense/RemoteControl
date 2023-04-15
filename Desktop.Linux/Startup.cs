using Immense.RemoteControl.Desktop.Linux.Services;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Extensions;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.ViewModels;
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
        /// Adds remote control services to a console app.  This will also apply command line
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
            Func<IServiceProvider, Task>? startupConfig = null,
            string serverUri = "")
        {
            var services = new ServiceCollection();

            serviceConfig?.Invoke(services);

            return await services.BuildRemoteControlServiceProvider(args, clientConfig, AddLinuxServices, startupConfig, serverUri);
        }


        private static void AddLinuxServices(IServiceCollection services)
        {
            services.AddSingleton<ICursorIconWatcher, CursorIconWatcherLinux>();
            services.AddSingleton<IKeyboardMouseInput, KeyboardMouseInputLinux>();
            services.AddSingleton<IClipboardService, ClipboardServiceLinux>();
            services.AddSingleton<IAudioCapturer, AudioCapturerLinux>();
            services.AddSingleton<IChatUiService, ChatUiServiceLinux>();
            services.AddTransient<IScreenCapturer, ScreenCapturerLinux>();
            services.AddScoped<IFileTransferService, FileTransferServiceLinux>();
            services.AddSingleton<ISessionIndicator, SessionIndicatorLinux>();
            services.AddSingleton<IShutdownService, ShutdownServiceLinux>();
            services.AddScoped<IRemoteControlAccessService, RemoteControlAccessServiceLinux>();
            services.AddSingleton<IAvaloniaDispatcher, AvaloniaDispatcher>();
            services.AddSingleton<IAppStartup, AppStartup>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
            services.AddTransient<IMessageBoxViewModel, MessageBoxViewModel>();
        }
    }
}
