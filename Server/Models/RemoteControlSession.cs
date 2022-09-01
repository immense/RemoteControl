using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Models
{
    public class RemoteControlSession
    {
        public static RemoteControlSession Empty { get; } = new();

        public string AccessKey { get; internal set; } = string.Empty;
        public string AttendedSessionId { get; internal set; } = string.Empty;
        public string DesktopConnectionId { get; internal set; } = string.Empty;
        public string MachineName { get; internal set; } = string.Empty;
        public RemoteControlMode Mode { get; internal set; }
        public string OrganizationName { get; internal set; }
        public string RequesterName { get; internal set; } = string.Empty;
        public string RequesterUserName { get; internal set; } = string.Empty;
        public DateTimeOffset StartTime { get; internal set; }
        public string UnattendedSessionId { get; internal set; } = string.Empty;

        /// <summary>
        /// Contains a collection of viewer SignalR connection IDs.
        /// </summary>
        public HashSet<string> ViewerList { get; } = new();
    }
}
