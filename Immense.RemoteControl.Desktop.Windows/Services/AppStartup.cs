using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using Immense.RemoteControl.Desktop.UI.WPF.Views;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Immense.RemoteControl.Desktop.Windows.Services;

internal class AppStartup : IAppStartup
{
    private readonly IAppState _appState;
    private readonly IKeyboardMouseInput _inputService;
    private readonly IDesktopHubConnection _desktopHub;
    private readonly IClipboardService _clipboardService;
    private readonly IChatHostService _chatHostService;
    private readonly ICursorIconWatcher _cursorIconWatcher;
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IMainWindowViewModel _mainWindowVm;
    private readonly IIdleTimer _idleTimer;
    private readonly IShutdownService _shutdownService;
    private readonly ILogger<AppStartup> _logger;
    private MainWindow? _mainWindow;

    public AppStartup(
        IMainWindowViewModel mainWindowVm,
        IAppState appState,
        IKeyboardMouseInput inputService,
        IDesktopHubConnection desktopHub,
        IClipboardService clipboardService,
        IChatHostService chatHostService,
        ICursorIconWatcher iconWatcher,
        IWindowsUiDispatcher dispatcher,
        IIdleTimer idleTimer,
        IShutdownService shutdownService,
        ILogger<AppStartup> logger)
    {
        _appState = appState;
        _inputService = inputService;
        _desktopHub = desktopHub;
        _clipboardService = clipboardService;
        _chatHostService = chatHostService;
        _cursorIconWatcher = iconWatcher;
        _dispatcher = dispatcher;
        _mainWindowVm = mainWindowVm;
        _idleTimer = idleTimer;
        _shutdownService = shutdownService;
        _logger = logger;
    }

    public async Task Initialize()
    {
        _dispatcher.StartWinFormsThread();

        var wpfStarted = await _dispatcher.StartWpfThread().ConfigureAwait(false);

        if (!wpfStarted)
        {
            throw new Exception("WPF app thread failed to start.");
        }

        _logger.LogInformation("Background WPF thread started.");

        if (_appState.Mode is AppMode.Unattended or AppMode.Attended)
        {
            _clipboardService.BeginWatching();
            _inputService.Init();
            _cursorIconWatcher.OnChange += CursorIconWatcher_OnChange;
        }

        switch (_appState.Mode)
        {
            case AppMode.Unattended:
                _dispatcher.InvokeWpf(() =>
                {
                    System.Windows.Application.Current!.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                });
                await StartScreenCasting().ConfigureAwait(false);
                break;
            case AppMode.Attended:
                _dispatcher.InvokeWpf(() =>
                {
                    _mainWindow = new MainWindow
                    {
                        DataContext = _mainWindowVm
                    };
                    _mainWindow.Show();
                });
                break;
            case AppMode.Chat:
                await _chatHostService
                    .StartChat(_appState.PipeName, _appState.OrganizationName)
                    .ConfigureAwait(false);
                break;
            default:
                break;
        }
    }


    private async Task StartScreenCasting()
    {
        if (!await _desktopHub.Connect(_dispatcher.ApplicationExitingToken, TimeSpan.FromSeconds(30)))
        {
            await _shutdownService.Shutdown();
            return;
        }

        var result = await _desktopHub.SendUnattendedSessionInfo(
            _appState.SessionId,
            _appState.AccessKey,
            Environment.MachineName,
            _appState.RequesterName,
            _appState.OrganizationName);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Exception, "An error occurred while trying to establish a session with the server.");
            await _shutdownService.Shutdown();
            return;
        }

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
            await _desktopHub.NotifyRequesterUnattendedReady();
        }

        _idleTimer.Start();
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
