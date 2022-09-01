using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Abstractions
{
    public interface IViewerHubDataProvider
    {
        bool EnforceAttendedAccess { get; }
        bool RemoteControlNotifyUser { get; }
    }
}
