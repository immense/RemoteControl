namespace Immense.RemoteControl.Shared.Models;

public class ScreenCastRequest
{
    public bool NotifyUser { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string ViewerID { get; set; } = string.Empty;
    public Guid StreamId { get; set; }
}
