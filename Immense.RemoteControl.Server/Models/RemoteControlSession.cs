namespace Immense.RemoteControl.Server.Models;

public class RemoteControlSession
{
    public RemoteControlSession()
    {
        Created = DateTimeOffset.Now;
    }

    public static RemoteControlSession Empty { get; } = new();
    public string AccessKey { get; internal set; } = string.Empty;
    public string RelativeAccessLink => $"/RemoteControl/Viewer?mode=Unattended&sessionId={UnattendedSessionId}&accessKey={AccessKey}&viewonly=False";
    public string AttendedSessionId { get; set; } = string.Empty;
    public DateTimeOffset Created { get; }
    public string DesktopConnectionId { get; internal set; } = string.Empty;
    public string MachineName { get; internal set; } = string.Empty;
    public RemoteControlMode Mode { get; internal set; }
    public string OrganizationName { get; internal set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterUserName { get; internal set; } = string.Empty;
    public DateTimeOffset StartTime { get; internal set; }
    public Guid StreamId { get; internal set; }
    public Guid UnattendedSessionId { get; set; }

    /// <summary>
    /// Contains a collection of viewer SignalR connection IDs.
    /// </summary>
    public HashSet<string> ViewerList { get; } = new();
}
