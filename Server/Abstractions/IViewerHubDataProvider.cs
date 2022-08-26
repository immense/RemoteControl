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
        bool DoesUserHaveAccessToDevice(string targetDeviceId, string? userIdentifier);
        int GetConcurrentSessionLimit();
        string GetOrganizationNameById(string orgId);
        string GetRequesterDisplayName(string? userIdentifier);
        string GetRequesterOrganizationId(string? userIdentifier);
        bool OtpMatchesDevice(string otp, string targetDeviceId);
    }
}
