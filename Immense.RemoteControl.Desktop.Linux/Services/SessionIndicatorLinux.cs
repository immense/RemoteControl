using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.Views;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class SessionIndicatorLinux : ISessionIndicator
{
    private readonly IUiDispatcher _dispatcher;

    public SessionIndicatorLinux(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }
    public void Show()
    {
        _dispatcher.Post(() =>
        {
            var indicatorWindow = new SessionIndicatorWindow();
            indicatorWindow.Show();
        });
    }
}
