namespace Immense.RemoteControl.Server.Models;

public class RemoteControlSession
{
    private readonly ManualResetEventSlim _sessionReadySignal = new(false);

    public RemoteControlSession()
    {
        Created = DateTimeOffset.Now;
    }

    public static RemoteControlSession Empty { get; } = new();
    public string AccessKey { get; internal set; } = string.Empty;
    public string AttendedSessionId { get; set; } = string.Empty;
    public DateTimeOffset Created { get; }
    public string DesktopConnectionId { get; internal set; } = string.Empty;
    public string MachineName { get; internal set; } = string.Empty;
    public RemoteControlMode Mode { get; internal set; }
    public string OrganizationName { get; internal set; } = string.Empty;
    public string RelativeAccessUri => $"/RemoteControl/Viewer?mode=Unattended&sessionId={UnattendedSessionId}&accessKey={AccessKey}&viewonly=False";
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterUserName { get; internal set; } = string.Empty;
    public DateTimeOffset StartTime { get; internal set; }
    public Guid StreamId { get; internal set; }
    public Guid UnattendedSessionId { get; set; }

    /// <summary>
    /// Contains a collection of viewer SignalR connection IDs.
    /// </summary>
    public HashSet<string> ViewerList { get; } = new();

    public Task<bool> WaitForSessionReady(TimeSpan waitTime)
    {
        return Task.Run(() => _sessionReadySignal.Wait(waitTime));
    }

    public Task<bool> WaitForSessionReady(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                _sessionReadySignal.Wait(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    internal void SetSessionReadyState(bool isReady)
    {
        if (isReady)
        {
            _sessionReadySignal.Set();
        }
        else
        {
            _sessionReadySignal.Reset();
        }
    }
}
