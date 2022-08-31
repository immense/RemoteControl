using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.Win32;
using Immense.RemoteControl.Desktop.Windows.ViewModels;
using Immense.RemoteControl.Desktop.Windows.Views;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    internal class AppStartup : IAppStartup
    {
        private readonly Form _backgroundForm;
        private readonly IAppState _appState;
        private readonly IKeyboardMouseInput _inputService;
        private readonly IDesktopHubConnection _desktopHub;
        private readonly IClipboardService _clipboardService;
        private readonly IChatHostService _chatHostService;
        private readonly ICursorIconWatcher _cursorIconWatcher;
        private readonly IWpfDispatcher _dispatcher;
        private readonly MainWindowViewModel _mainWindowVm;
        private readonly IIdleTimer _idleTimer;
        private readonly ILogger<AppStartup> _logger;
        private MainWindow? _mainWindow;

        public AppStartup(
            Form backgroundForm,
            MainWindowViewModel mainWindowVm,
            IAppState appState,
            IKeyboardMouseInput inputService,
            IDesktopHubConnection desktopHub,
            IClipboardService clipboardService,
            IChatHostService chatHostService,
            ICursorIconWatcher iconWatcher,
            IWpfDispatcher dispatcher,
            IIdleTimer idleTimer,
            ILogger<AppStartup> logger)
        {
            _backgroundForm = backgroundForm;
            _appState = appState;
            _inputService = inputService;
            _desktopHub = desktopHub;
            _clipboardService = clipboardService;
            _chatHostService = chatHostService;
            _cursorIconWatcher = iconWatcher;
            _dispatcher = dispatcher;
            _mainWindowVm = mainWindowVm;
            _idleTimer = idleTimer;
            _logger = logger;
        }

        public async Task Initialize()
        {
            SystemEvents.SessionEnding += async (s, e) =>
            {
                if (e.Reason == SessionEndReasons.SystemShutdown)
                {
                    await _desktopHub.DisconnectAllViewers();
                }
            };

            StartWinFormsThread();

            if (System.Windows.Application.Current is null)
            {
                _dispatcher.StartWpfThread();

                _logger.LogInformation("Background WPF thread started.");
            }

            if (_appState.Mode is AppMode.Unattended or AppMode.Attended)
            {
                _clipboardService.BeginWatching();
                _inputService.Init();
                _cursorIconWatcher.OnChange += CursorIconWatcher_OnChange;
            }

            switch (_appState.Mode)
            {
                case AppMode.Unattended:
                    _dispatcher.Invoke(() =>
                    {
                        System.Windows.Application.Current!.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    });
                    await StartScreenCasting().ConfigureAwait(false);
                    break;
                case AppMode.Attended:
                    _dispatcher.Invoke(() =>
                    {
                        _mainWindow = new MainWindow
                        {
                            DataContext = _mainWindowVm
                        };
                        _mainWindow.Show();
                    });
                    break;
                case AppMode.Chat:
                    await _chatHostService.StartChat(_appState.RequesterConnectionId, _appState.OrganizationName)
                        .ConfigureAwait(false);
                    break;
                default:
                    break;
            }
        }


        private async Task StartScreenCasting()
        {
            await _desktopHub.Connect(_dispatcher.ApplicationExitingToken);
            await _desktopHub.SendDeviceInfo(_appState.ServiceConnectionId, Environment.MachineName, _appState.DeviceID);

            if (Win32Interop.GetCurrentDesktop(out var currentDesktopName))
            {
                _logger.LogInformation("Setting initial desktop to {currentDesktopName}.", currentDesktopName);
            }
            else
            {
                _logger.LogWarning("Failed to get initial desktop name.");
            }

            if (!Win32Interop.SwitchToInputDesktop())
            {
                _logger.LogWarning("Failed to set initial desktop.");
            }

            if (_appState.ArgDict.ContainsKey("relaunch"))
            {
                _logger.LogInformation("Resuming after relaunch.");
                var viewersString = _appState.ArgDict["viewers"];
                var viewerIDs = viewersString.Split(",".ToCharArray());
                await _desktopHub.NotifyViewersRelaunchedScreenCasterReady(viewerIDs);
            }
            else
            {
                await _desktopHub.NotifyRequesterUnattendedReady(_appState.RequesterConnectionId);
            }

            _idleTimer.Start();
        }

        private void StartWinFormsThread()
        {
            var winformsThread = new Thread(() =>
            {
                System.Windows.Forms.Application.Run(_backgroundForm);
            })
            {
                IsBackground = true
            };
            winformsThread.TrySetApartmentState(ApartmentState.STA);
            winformsThread.Start();

            _logger.LogInformation("Background WinForms thread started.");
        }

        private async void CursorIconWatcher_OnChange(object? sender, CursorInfo cursor)
        {
            if (_appState?.Viewers?.Any() == true)
            {
                foreach (var viewer in _appState.Viewers.Values)
                {
                    await viewer.SendCursorChange(cursor);
                }
            }
        }
    }
}
