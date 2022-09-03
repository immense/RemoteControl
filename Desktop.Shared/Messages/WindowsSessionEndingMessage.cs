using Immense.RemoteControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Messages
{
    public class WindowsSessionEndingMessage
    {
        public WindowsSessionEndingMessage(SessionEndReasonsEx reason)
        {
            Reason = reason;
        }

        public SessionEndReasonsEx Reason { get; }
    }
}
