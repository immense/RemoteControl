using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Models
{
    public class RestartScreenCasterRequiredModel
    {
        public HashSet<string> ViewerList { get; init; } = new();
        public string ServiceConnectionId { get; init; } = string.Empty;
        public string DesktopConnectionId { get; init; } = string.Empty;
    }
}
