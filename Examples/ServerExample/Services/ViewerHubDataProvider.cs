using Immense.RemoteControl.Server.Abstractions;

namespace ServerExample.Services;

internal class ViewerHubDataProvider : IViewerHubDataProvider
{
    public bool EnforceAttendedAccess => false;

    public bool RemoteControlNotifyUser => true;

}