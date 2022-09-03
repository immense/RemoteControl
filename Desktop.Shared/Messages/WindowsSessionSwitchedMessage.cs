using Immense.RemoteControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Messages
{
    public class WindowsSessionSwitched
    {
        public WindowsSessionSwitched(SessionSwitchReasonEx reason, int sessionId)
        {
            Reason = reason;
            SessionId = sessionId;
        }

        public SessionSwitchReasonEx Reason { get; }
        public int SessionId { get; }
    }
}
