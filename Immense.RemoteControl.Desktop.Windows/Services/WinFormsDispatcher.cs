using Immense.RemoteControl.Desktop.Shared.Messages;
using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using Immense.SimpleMessenger;
using Immense.RemoteControl.Desktop.UI.Services;

namespace Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Services;

public interface IWinFormsDispatcher
{
    void InvokeWinForms(Action action);
    void StartWinFormsThread();
}

public class WinFormsDispatcher : IWinFormsDispatcher
{
    private readonly ILogger<WinFormsDispatcher> _logger;
    private readonly IMessenger _messenger;
    private Form? _backgroundForm;
    private Thread? _winformsThread;

    public WinFormsDispatcher(
        IMessenger messenger,
        IUiDispatcher uiDispatcher,
        ILogger<WinFormsDispatcher> logger)
    {
        _messenger = messenger;
        _logger = logger;
        uiDispatcher.ApplicationExitingToken.Register(Shutdown);
    }

    public void InvokeWinForms(Action action)
    {
        _backgroundForm?.Invoke(action);
    }

    public void StartWinFormsThread()
    {
        _backgroundForm = new Form()
        {
            Visible = false,
            Opacity = 0,
            ShowIcon = false,
            ShowInTaskbar = false,
            WindowState = FormWindowState.Minimized
        };
        _backgroundForm.Load += BackgroundForm_Load;
        _backgroundForm.FormClosing += BackgroundForm_FormClosing;
        _winformsThread = new Thread(() =>
        {
            Application.Run(_backgroundForm);
        })
        {
            IsBackground = true
        };
        _winformsThread.TrySetApartmentState(ApartmentState.STA);
        _winformsThread.Start();

        _logger.LogInformation("Background WinForms thread started.");
    }

    private void BackgroundForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
    }

    private void BackgroundForm_Load(object? sender, EventArgs e)
    {
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        SystemEvents.SessionEnding += SystemEvents_SessionEnding;
    }

    private void Shutdown()
    {
        try
        {
            _logger.LogInformation("Shutting down WinForms thread.");
            Application.Exit();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while shutting down WinForms thread.");
        }
    }

    private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
    {
        _logger.LogInformation("Session ending.  Reason: {reason}", e.Reason);

        var reason = (SessionEndReasonsEx)e.Reason;
        _messenger.Send(new WindowsSessionEndingMessage(reason));
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        _logger.LogInformation("Session changing.  Reason: {reason}", e.Reason);

        var reason = (SessionSwitchReasonEx)(int)e.Reason;
        _messenger.Send(new WindowsSessionSwitchedMessage(reason, Process.GetCurrentProcess().SessionId));
    }
}
