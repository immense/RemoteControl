using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Messages
{
    public class AppStateHostChangedMessage
    {
        public AppStateHostChangedMessage(string newHost)
        {
            NewHost = newHost;
        }

        public string NewHost { get; }
    }
}
