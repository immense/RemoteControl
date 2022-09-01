using Immense.RemoteControl.Server.Abstractions;

namespace ServerExample.Services
{
    internal class ViewerHubDataProvider : IViewerHubDataProvider
    {
        public bool EnforceAttendedAccess => false;

        public bool RemoteControlNotifyUser => true;

        public int RemoteControlSessionLimit => 8;

        public bool DoesUserHaveAccessToDevice(string targetDeviceId, string? userIdentifier)
        {
            return true;
        }

        public string GetOrganizationNameById(string orgId)
        {
            return "No idea";
        }

        public string GetRequesterDisplayName(string? userIdentifier)
        {
            return "Admiral Ackbar";
        }

        public string GetRequesterOrganizationId(string? userIdentifier)
        {
            return Guid.NewGuid().ToString();
        }

        public bool OtpMatchesDevice(string otp, string targetDeviceId)
        {
            return true;
        }
    }
}