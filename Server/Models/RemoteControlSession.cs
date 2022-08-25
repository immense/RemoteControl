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

        public string AttendedSessionID { get; set; } = string.Empty;
        public string CasterSocketID { get; set; } = string.Empty;
        public string DeviceID { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public RemoteControlMode Mode { get; set; }
        public string OrganizationID { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterSocketID { get; set; } = string.Empty;
        public string RequesterUserName { get; set; } = string.Empty;
        public string ServiceID { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public HashSet<string> ViewerList { get; } = new();
    }
}
