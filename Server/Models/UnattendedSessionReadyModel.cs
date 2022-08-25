using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Models
{
    public class UnattendedSessionReadyModel
    {
        public string DeviceId { get; init; } = string.Empty;
        public string BrowserConnectionId { get; init; } = string.Empty;
        public string DesktopConnectionId { get; init; } = string.Empty;
    }
}
