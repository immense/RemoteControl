using Immense.RemoteControl.Desktop.UI.WPF.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Immense.RemoteControl.Shared.Models;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Desktop.Shared.Reactive;
using Microsoft.Extensions.DependencyInjection;
using Immense.RemoteControl.Desktop.Native.Windows;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels;

public interface IMainWindowViewModel
{
    bool CanElevateToAdmin { get; }
    bool CanElevateToService { get; }
    AsyncRelayCommand ChangeServerCommand { get; }
    RelayCommand ElevateToAdminCommand { get; }
    RelayCommand ElevateToServiceCommand { get; }
    string Host { get; set; }
    bool IsAdministrator { get; }
    AsyncRelayCommand<IList<object>> RemoveViewersCommand { get; }
    string SessionId { get; set; }
    string StatusMessage { get; set; }
    ObservableCollection<IViewer> Viewers { get; }

    bool CanRemoveViewers(IList<object>? items);
    void CopyLink();
    Task Init();
    void ShutdownApp();
}

public class MainWindowViewModel : BrandedViewModelBase, IMainWindowViewModel
{
    private readonly IAppState _appState;
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IDesktopHubConnection _hubConnection;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IScreenCaster _screenCaster;
    private readonly IShutdownService _shutdownService;
    private readonly IViewModelFactory _viewModelFactory;

    public MainWindowViewModel(
        IBrandingProvider brandingProvider,
        IWindowsUiDispatcher dispatcher,
        IAppState appState,
        IDesktopHubConnection hubConnection,
        IScreenCaster screenCaster,
        IShutdownService shutdownService,
        IViewModelFactory viewModelFactory,
        ILogger<MainWindowViewModel> logger)
        : base(brandingProvider, dispatcher, logger)
    {
        _dispatcher = dispatcher;
        _appState = appState;
        _hubConnection = hubConnection;
        _screenCaster = screenCaster;
        _shutdownService = shutdownService;
        _viewModelFactory = viewModelFactory;
        _logger = logger;

        _appState.ViewerRemoved += ViewerRemoved;
        _appState.ViewerAdded += ViewerAdded;
        _appState.ScreenCastRequested += ScreenCastRequested;

        Host = appState.Host;
        ChangeServerCommand = new AsyncRelayCommand(ChangeServer);
        ElevateToAdminCommand = new RelayCommand(ElevateToAdmin, () => CanElevateToAdmin);
        ElevateToServiceCommand = new RelayCommand(ElevateToService, () => CanElevateToService);
        RemoveViewersCommand = new AsyncRelayCommand<IList<object>>(RemoveViewers, CanRemoveViewers);
    }


    public bool CanElevateToAdmin => !IsAdministrator;

    public bool CanElevateToService => IsAdministrator && !WindowsIdentity.GetCurrent().IsSystem;

    public AsyncRelayCommand ChangeServerCommand { get; }

    public RelayCommand ElevateToAdminCommand { get; }

    public RelayCommand ElevateToServiceCommand { get; }

    public string Host
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value?.Trim()?.TrimEnd('/'));
    }

    public bool IsAdministrator { get; } = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public AsyncRelayCommand<IList<object>> RemoveViewersCommand { get; }

    public string SessionId
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public string StatusMessage
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }
    public ObservableCollection<IViewer> Viewers { get; } = new();

    public bool CanRemoveViewers(IList<object>? items) => items?.Any() == true;


    public void CopyLink()
    {
        try
        {
            Clipboard.SetDataObject($"{Host}/RemoteControl/Viewer?sessionId={StatusMessage?.Replace(" ", "")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while copying attended session link to clipboard.");
        }
    }

    public async Task Init()
    {
        _dispatcher.CurrentApp.Exit -= Application_Exit;
        _dispatcher.CurrentApp.Exit += Application_Exit;

        StatusMessage = "Retrieving...";

        while (string.IsNullOrWhiteSpace(Host))
        {
            Host = "https://";
            PromptForHostName();
        }

        _appState.Host = Host;
        _appState.Mode = Shared.Enums.AppMode.Attended;

        try
        {
            var result = await _hubConnection.Connect(_dispatcher.ApplicationExitingToken, TimeSpan.FromSeconds(10));

            if (result && _hubConnection.Connection is not null)
            {
                _hubConnection.Connection.Closed += (ex) =>
                {
                    _dispatcher.InvokeWpf(() =>
                    {
                        Viewers.Clear();
                        StatusMessage = "Disconnected";
                    });
                    return Task.CompletedTask;
                };

                _hubConnection.Connection.Reconnecting += (ex) =>
                {
                    _dispatcher.InvokeWpf(() =>
                    {
                        Viewers.Clear();
                        StatusMessage = "Reconnecting";
                    });
                    return Task.CompletedTask;
                };

                _hubConnection.Connection.Reconnected += async (id) =>
                {
                    await GetSessionID();
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
        MessageBox.Show(_dispatcher.CurrentApp.MainWindow, "Failed to connect to server.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShutdownApp()
    {
        _shutdownService.Shutdown();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _dispatcher.InvokeWpf(() =>
        {
            Viewers.Clear();
        });
    }

    private async Task ChangeServer()
    {
        PromptForHostName();
        await Init();
    }

    private void ElevateToAdmin()
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


    private void ElevateToService()
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

            psi.Arguments = $"/c sc create RemoteControl_Temp binPath=\"{filePath} {arguments} --elevate\"";
            Process.Start(psi)?.WaitForExit();
            psi.Arguments = "/c sc start RemoteControl_Temp";
            Process.Start(psi)?.WaitForExit();
            psi.Arguments = "/c sc delete RemoteControl_Temp";
            Process.Start(psi)?.WaitForExit();
            _dispatcher.CurrentApp.Shutdown();
        }
        catch { }
    }
    private async Task GetSessionID()
    {
        var sessionId = await _hubConnection.GetSessionID();
        await _hubConnection.SendAttendedSessionInfo(Environment.MachineName);

        var formattedSessionID = "";
        for (var i = 0; i < sessionId.Length; i += 3)
        {
            formattedSessionID += $"{sessionId.Substring(i, 3)} ";
        }

        _dispatcher.InvokeWpf(() =>
        {
            SessionId = formattedSessionID.Trim();
            StatusMessage = SessionId;
        });
    }

    private void PromptForHostName()
    {
        var viewModel = _viewModelFactory.CreateHostNamePromptViewModel();
        var prompt = new HostNamePrompt(viewModel);

        if (!string.IsNullOrWhiteSpace(Host))
        {
            viewModel.Host = Host;
        }

        prompt.Owner = _dispatcher.CurrentApp.MainWindow;
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

    private async Task RemoveViewers(IList<object>? viewers)
    {
        if (viewers?.Any() != true)
        {
            return;
        }

        foreach (var viewer in viewers.OfType<Viewer>().ToArray())
        {
            ViewerRemoved(this, viewer.ViewerConnectionID);
            await _hubConnection.DisconnectViewer(viewer, true);
        }
    }
    private async void ScreenCastRequested(object? sender, ScreenCastRequest screenCastRequest)
    {
        await _dispatcher.InvokeWpfAsync(async () =>
        {
            _dispatcher.CurrentApp.MainWindow.Activate();
            var result = MessageBox.Show(_dispatcher.CurrentApp.MainWindow, $"You've received a connection request from {screenCastRequest.RequesterName}.  Accept?", "Connection Request", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
        _dispatcher.InvokeWpf(() =>
        {
            Viewers.Add(viewer);
        });
    }

    private void ViewerRemoved(object? sender, string viewerID)
    {
        _dispatcher.InvokeWpf(() =>
        {
            var viewer = Viewers.FirstOrDefault(x => x.ViewerConnectionID == viewerID);
            if (viewer != null)
            {
                Viewers.Remove(viewer);
            }
        });
    }
}
