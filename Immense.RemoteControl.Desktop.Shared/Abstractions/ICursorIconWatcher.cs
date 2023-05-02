using Immense.RemoteControl.Shared.Models;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface ICursorIconWatcher
{
    event EventHandler<CursorInfo> OnChange;

    CursorInfo GetCurrentCursor();
}
