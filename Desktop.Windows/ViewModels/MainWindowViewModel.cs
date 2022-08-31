using Microsoft.Extensions.DependencyInjection;
using Immense.RemoteControl.Desktop.Windows.Services;
using Immense.RemoteControl.Desktop.Windows.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using WpfApp = System.Windows.Application;
using Immense.RemoteControl.Shared.Models;
using CommunityToolkit.Mvvm.Input;
using Clipboard = System.Windows.Clipboard;
using Immense.RemoteControl.Desktop.Shared.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels
{
    public partial class MainWindowViewModel : BrandedViewModelBase
    {
        private readonly IBrandingProvider _brandingProvider;
        private readonly IDesktopHubConnection _hubConnection;
        private readonly IScreenCaster _screenCaster;
        private readonly IShutdownService _shutdownService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IAppState _appState;
        private readonly IWpfDispatcher _dispatcher;
        private readonly ICursorIconWatcher _cursorIconWatcher;
        private readonly ILogger<MainWindowViewModel> _logger;

        [ObservableProperty]
        private string _host = string.Empty;

        [ObservableProperty]
        private string _sessionId = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public MainWindowViewModel(
            IBrandingProvider brandingProvider,
            IWpfDispatcher dispatcher,
            ICursorIconWatcher iconWatcher,
            IAppState appState,
            IDesktopHubConnection hubConnection,
            IScreenCaster screenCaster,
            IShutdownService shutdownService,
            IViewModelFactory viewModelFactory,
            ILogger<MainWindowViewModel> logger)
            : base(brandingProvider, dispatcher, logger)
        {
            WpfApp.Current.Exit += Application_Exit;

            _brandingProvider = brandingProvider;
            _dispatcher = dispatcher;
            _cursorIconWatcher = iconWatcher;
            _cursorIconWatcher.OnChange += CursorIconWatcher_OnChange;
            _appState = appState;
            _hubConnection = hubConnection;
            _screenCaster = screenCaster;
            _shutdownService = shutdownService;
            _viewModelFactory = viewModelFactory;
            _logger = logger;

            _appState.ViewerRemoved += ViewerRemoved;
            _appState.ViewerAdded += ViewerAdded;
            _appState.ScreenCastRequested += ScreenCastRequested;
        }

        [RelayCommand]
        public async Task ChangeServer()
        {
            PromptForHostName();
            await Init();
        }

        [RelayCommand(CanExecute = nameof(CanElevateToAdmin))]
        public void ElevateToAdmin()
        {
            try
            {
                //var filePath = Process.GetCurrentProcess().MainModule.FileName;
                var commandLine = Win32Interop.GetCommandLine().Replace(" --elevate", "");
                var sections = commandLine.Split('"', StringSplitOptions.RemoveEmptyEntries);
                var filePath = sections.First();
                var arguments = string.Join('"', sections.Skip(1));
                var psi = new ProcessStartInfo(filePath, arguments)
                {
                    Verb = "RunAs",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
                Environment.Exit(0);
            }
            // Exception can be thrown if UAC is dialog is cancelled.
            catch { }
        }


        [RelayCommand(CanExecute = nameof(CanElevateToService))]
        public void ElevateToService()
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                //var filePath = Process.GetCurrentProcess().MainModule.FileName;
                var commandLine = Win32Interop.GetCommandLine().Replace(" --elevate", "");
                var sections = commandLine.Split('"', StringSplitOptions.RemoveEmptyEntries);
                var filePath = sections.First();
                var arguments = string.Join('"', sections.Skip(1));

                _logger.LogInformation("Creating temporary service with file path {filePath} and arguments {arguments}.",
                    filePath,
                    arguments);

                psi.Arguments = $"/c sc create Remotely_Temp binPath=\"{filePath} {arguments} --elevate\"";
                Process.Start(psi)?.WaitForExit();
                psi.Arguments = "/c sc start RemoteControl_Temp";
                Process.Start(psi)?.WaitForExit();
                psi.Arguments = "/c sc delete RemoteControl_Temp";
                Process.Start(psi)?.WaitForExit();
                WpfApp.Current.Shutdown();
            }
            catch { }
        }

        public bool CanElevateToService => IsAdministrator && !WindowsIdentity.GetCurrent().IsSystem;
     
        public bool IsAdministrator { get; } = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public bool CanElevateToAdmin => !IsAdministrator;

        [RelayCommand(CanExecute = nameof(CanRemoveViewers))]
        public async Task RemoveViewers(IList<object> viewers)
        {
            foreach (var viewer in viewers.OfType<Viewer>().ToArray())
            {
                ViewerRemoved(this, viewer.ViewerConnectionID);
                await _hubConnection.DisconnectViewer(viewer, true);
            }
        }

        public bool CanRemoveViewers(IList<object> items) => items.Any();

        public ObservableCollection<IViewer> Viewers { get; } = new();

        public void CopyLink()
        {
            Clipboard.SetText($"{Host}/RemoteControl/Viewer?sessionID={StatusMessage?.Replace(" ", "")}");
        }

        public async Task GetSessionID()
        {
            await _hubConnection.SendDeviceInfo(_appState.ServiceConnectionId, Environment.MachineName, _appState.DeviceID);
            var sessionId = await _hubConnection.GetSessionID();

            var formattedSessionID = "";
            for (var i = 0; i < sessionId.Length; i += 3)
            {
                formattedSessionID += sessionId.Substring(i, 3) + " ";
            }

            _dispatcher.Invoke(() =>
            {
                _sessionId = formattedSessionID.Trim();
                StatusMessage = _sessionId;
            });
        }

        public async Task Init()
        {
            StatusMessage = "Retrieving...";

            Host = _appState.Host;

            while (string.IsNullOrWhiteSpace(Host))
            {
                Host = "https://";
                PromptForHostName();
            }

            _appState.Host = Host;
            _appState.Mode = Shared.Enums.AppMode.Attended;

            try
            {
                var result = await _hubConnection.Connect(_dispatcher.ApplicationExitingToken);

                if (result)
                {
                    _hubConnection.Connection.Closed += (ex) =>
                    {
                        _dispatcher.Invoke(() =>
                        {
                            Viewers.Clear();
                            StatusMessage = "Disconnected";
                        });
                        return Task.CompletedTask;
                    };

                    _hubConnection.Connection.Reconnecting += (ex) =>
                    {
                        _dispatcher.Invoke(() =>
                        {
                            Viewers.Clear();
                            StatusMessage = "Reconnecting";
                        });
                        return Task.CompletedTask;
                    };

                    _hubConnection.Connection.Reconnected += (id) =>
                    {
                        StatusMessage = _sessionId;
                        return Task.CompletedTask;
                    };

                    await ApplyBranding();

                    await GetSessionID();

                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initialization.");
            }

            // If we got here, something went wrong.
            StatusMessage = "Failed";
            MessageBox.Show(Application.Current.MainWindow, "Failed to connect to server.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void PromptForHostName()
        {
            var prompt = new HostNamePrompt();
            var viewModel = _viewModelFactory.CreateHostNamePromptViewModel();
            prompt.DataContext = viewModel;

            if (!string.IsNullOrWhiteSpace(Host))
            {
                viewModel.Host = Host;
            }

            prompt.Owner = Application.Current.MainWindow;
            prompt.ShowDialog();
            var result = viewModel.Host?.Trim()?.TrimEnd('/');

            if (!Uri.TryCreate(result, UriKind.Absolute, out var serverUri) ||
                serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Server URL is not valid.");
                MessageBox.Show("Server URL must be a valid Uri (e.g. https://example.com).", "Invalid Server URL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Host = result;
            _appState.Host = Host;
        }

        public void ShutdownApp()
        {
            _shutdownService.Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                Viewers.Clear();
            });
        }

        private async void CursorIconWatcher_OnChange(object? sender, CursorInfo cursor)
        {
            if (_appState?.Viewers?.Count > 0)
            {
                foreach (var viewer in _appState.Viewers.Values)
                {
                    await viewer.SendCursorChange(cursor);
                }
            }
        }

        private async void ScreenCastRequested(object? sender, ScreenCastRequest screenCastRequest)
        {
            await _dispatcher.InvokeAsync(async () =>
            {
                WpfApp.Current.MainWindow.Activate();
                var result = MessageBox.Show(WpfApp.Current.MainWindow, $"You've received a connection request from {screenCastRequest.RequesterName}.  Accept?", "Connection Request", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _screenCaster.BeginScreenCasting(screenCastRequest);
                }
                else
                {
                    await _hubConnection.SendConnectionRequestDenied(screenCastRequest.ViewerID);
                }
            });
        }

        private void ViewerAdded(object? sender, IViewer viewer)
        {
            _dispatcher.Invoke(() =>
            {
                Viewers.Add(viewer);
            });
        }

        private void ViewerRemoved(object? sender, string viewerID)
        {
            _dispatcher.Invoke(() =>
            {
                var viewer = Viewers.FirstOrDefault(x => x.ViewerConnectionID == viewerID);
                if (viewer != null)
                {
                    Viewers.Remove(viewer);
                }
            });
        }
    }
}
