using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Services;
using Immense.RemoteControl.Desktop.Views;

namespace Immense.RemoteControl.Desktop.Services.Linux;

public class SessionIndicatorLinux : ISessionIndicator
{
    private readonly IAvaloniaDispatcher _dispatcher;

    public SessionIndicatorLinux(IAvaloniaDispatcher dispatcher)
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
