using Immense.RemoteControl.Shared.Models;
using System;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions
{
    public interface ICursorIconWatcher
    {
        event EventHandler<CursorInfo> OnChange;

        CursorInfo GetCurrentCursor();
    }

}
