namespace Immense.RemoteControl.Server.Abstractions;

public interface IViewerHubDataProvider
{
    bool EnforceAttendedAccess { get; }
    bool RemoteControlNotifyUser { get; }
}
