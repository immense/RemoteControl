using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Shared;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.IO;

namespace Immense.RemoteControl.Desktop.Windows.Services;

public class SessionIndicatorWin : ISessionIndicator
{
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IDesktopHubConnection _hubConnection;
    private readonly IBrandingProvider _brandingProvider;
    private readonly ILogger<SessionIndicatorWin> _logger;
    private Container? _container;
    private ContextMenuStrip? _contextMenuStrip;
    private NotifyIcon? _notifyIcon;

    public SessionIndicatorWin(
        IWindowsUiDispatcher dispatcher,
        IDesktopHubConnection hubConnection,
        IBrandingProvider brandingProvider,
        ILogger<SessionIndicatorWin> logger)
    {
        _dispatcher = dispatcher;
        _hubConnection = hubConnection;
        _brandingProvider = brandingProvider;
        _logger = logger;
    }

    public void Show()
    {
        try
        {
            if (_notifyIcon != null)
            {
                return;
            }

            _dispatcher.InvokeWpf(() =>
            {
                _dispatcher.CurrentApp.Exit += App_Exit;
            });

            _dispatcher.InvokeWinForms(async () =>
            {
                _container = new Container();
                _contextMenuStrip = new ContextMenuStrip(_container);
                _contextMenuStrip.Items.Add("Exit", null, ExitMenuItem_Click);

                Icon icon;

                var brandingInfo = await _brandingProvider.GetBrandingInfo();
                if (brandingInfo.Icon?.Any() == true)
                {
                    using var ms = new MemoryStream(brandingInfo.Icon);
                    using var bitmap = new Bitmap(ms);
                    icon = Icon.FromHandle(bitmap.GetHicon());
                }
                else
                {
                    var fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(fileName) &&
                        Icon.ExtractAssociatedIcon(fileName) is Icon fileIcon)
                    {
                        icon = fileIcon;
                    }
                    else
                    {
                        using var mrs = typeof(Result).Assembly.GetManifestResourceStream("Immense.RemoteControl.Shared.Assets.DefaultIcon.ico");
                        icon = new Icon(mrs!);
                    }
                }

                _notifyIcon = new NotifyIcon(_container)
                {
                    Icon = icon,
                    Text = "Remote Control Session",
                    BalloonTipIcon = ToolTipIcon.Info,
                    BalloonTipText = "A remote control session has started.",
                    BalloonTipTitle = "Remote Control Started",
                    ContextMenuStrip = _contextMenuStrip,
                    Visible = true
                };
                _notifyIcon.ShowBalloonTip(3000);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while showing session indicator.");
        }
    }

    private void App_Exit(object sender, EventArgs e)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon?.Dispose();
            _notifyIcon?.Icon?.Dispose();
        }
    }

    private async void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        await _hubConnection.DisconnectAllViewers();
    }
}
