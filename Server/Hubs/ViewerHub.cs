using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Filters;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Hubs
{
    [ServiceFilter(typeof(ViewerFilterAttribute))]
    public class ViewerHub : Hub
    {
        private readonly IHubEventHandler _hubEvents;
        private readonly IDesktopHubSessionCache _desktopSessionCache;
        private readonly IHubContext<DesktopHub> _desktopHub;
        private readonly ILogger<ViewerHub> _logger;

        public ViewerHub(
            IHubEventHandler hubEvents,
            IDesktopHubSessionCache desktopSessionCache,
            IHubContext<DesktopHub> desktopHub,
            ILogger<ViewerHub> logger)
        {
            _hubEvents = hubEvents;
            _desktopSessionCache = desktopSessionCache;
            _desktopHub = desktopHub;
            _logger = logger;
        }

        private RemoteControlMode Mode
        {
            get
            {
                if (Context.Items.TryGetValue(nameof(Mode), out var result) &&
                    result is RemoteControlMode mode)
                {
                    return mode;
                }
                return RemoteControlMode.Unknown;
            }
            set
            {
                Context.Items[nameof(Mode)] = value;
            }
        }

        private RemoteControlSession SessionInfo
        {
            get
            {
                if (Context.Items.TryGetValue(nameof(SessionInfo), out var result) &&
                    result is RemoteControlSession session)
                {
                    return session;
                }

                var newSession = new RemoteControlSession();
                Context.Items[nameof(SessionInfo)] = newSession;
                return newSession;
            }
            set
            {
                Context.Items[nameof(SessionInfo)] = value;
            }
        }

        private string RequesterName
        {
            get
            {
                if (Context.Items.TryGetValue(nameof(RequesterName), out var result) &&
                    result is string requesterName)
                {
                    return requesterName;
                }
                return string.Empty;
            }
            set
            {
                Context.Items[nameof(RequesterName)] = value;
            }
        }

        private string ScreenCasterID
        {
            get
            {
                if (Context.Items.TryGetValue(nameof(ScreenCasterID), out var result) &&
                      result is string casterId)
                {
                    return casterId;
                }
                return string.Empty;
            }
            set
            {
                Context.Items[nameof(ScreenCasterID)] = value;
            }
        }

        public async Task ChangeWindowsSession(int sessionID)
        {
            if (SessionInfo?.Mode == RemoteControlMode.Unattended)
            {
                await _hubEvents.ChangeWindowsSession(SessionInfo.ServiceID, Context.ConnectionId, sessionID);
            }
        }

        public Task SendDtoToClient(byte[] baseDto)
        {
            if (string.IsNullOrWhiteSpace(ScreenCasterID))
            {
                return Task.CompletedTask;
            }

            return _desktopHub.Clients.Client(ScreenCasterID).SendAsync("SendDtoToClient", baseDto, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (!string.IsNullOrWhiteSpace(ScreenCasterID))
            {
               await _desktopHub.Clients.Client(ScreenCasterID).SendAsync("ViewerDisconnected", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendScreenCastRequestToDevice(string screenCasterID, string requesterName, int remoteControlMode, string otp)
        {
            if (string.IsNullOrWhiteSpace(screenCasterID))
            {
                return;
            }

            if ((RemoteControlMode)remoteControlMode == RemoteControlMode.Attended)
            {
                if (!_desktopSessionCache.Sessions.Any(x => x.AttendedSessionID == screenCasterID))
                {
                    await Clients.Caller.SendAsync("SessionIDNotFound");
                    return;
                }

                screenCasterID = _desktopSessionCache.Sessions.First(x => x.AttendedSessionID == screenCasterID).CasterConnectionId;
            }

            if (!_desktopSessionCache.TryGet(screenCasterID, out var sessionInfo))
            {
                await Clients.Caller.SendAsync("SessionIDNotFound");
                return;
            }

            SessionInfo = sessionInfo;
            ScreenCasterID = screenCasterID;
            RequesterName = requesterName;
            Mode = (RemoteControlMode)remoteControlMode;

            string orgId = string.Empty;

            if (Context?.User?.Identity?.IsAuthenticated == true)
            {
                var user = DataService.GetUserByID(Context.UserIdentifier);

                if (string.IsNullOrWhiteSpace(RequesterName))
                {
                    RequesterName = user.UserOptions.DisplayName ?? user.UserName;
                }
                orgId = user.OrganizationID;

                var currentUsers = _desktopSessionCache.Sessions.Count(x =>
                    x.CasterConnectionId != screenCasterID &&
                    x.OrganizationID == orgId &&
                    x.ViewerList.Any());

                if (currentUsers >= AppConfig.RemoteControlSessionLimit)
                {
                    await Clients.Caller.SendAsync("ShowMessage", "Max number of concurrent sessions reached.");
                    Context.Abort();
                    return;
                }
                SessionInfo.OrganizationID = orgId;
                SessionInfo.RequesterUserName = Context.User.Identity.Name ?? string.Empty;
                SessionInfo.RequesterSocketID = Context.ConnectionId;
            }

            var logMessage = $"Remote control session requested.  " +
                                $"Login ID (if logged in): {Context?.User?.Identity?.Name}.  " +
                                $"Machine Name: {SessionInfo.MachineName}.  " +
                                $"Requester Name (if specified): {RequesterName}.  " +
                                $"Connection ID: {Context?.ConnectionId}. User ID: {Context?.UserIdentifier}.  " +
                                $"Screen Caster ID: {screenCasterID}.  " +
                                $"Mode: {(RemoteControlMode)remoteControlMode}.  " +
                                $"Requester IP Address: {Context?.GetHttpContext()?.Connection?.RemoteIpAddress}"

            _hubEvents.LogRemoteControlStarted(logMessage, orgId);

            if (Mode == RemoteControlMode.Unattended)
            {
                var targetDevice = AgentHub.ServiceConnections[SessionInfo.ServiceID];

                var useWebRtc = targetDevice.WebRtcSetting == WebRtcSetting.Default ?
                            AppConfig.UseWebRtc :
                            targetDevice.WebRtcSetting == WebRtcSetting.Enabled;


                SessionInfo.Mode = RemoteControlMode.Unattended;

                if ((!string.IsNullOrWhiteSpace(otp) &&
                        RemoteControlFilterAttribute.OtpMatchesDevice(otp, targetDevice.ID))
                    ||
                    (Context.User.Identity.IsAuthenticated &&
                        DataService.DoesUserHaveAccessToDevice(targetDevice.ID, Context.UserIdentifier)))
                {
                    var orgName = DataService.GetOrganizationNameById(orgId);
                    await CasterHubContext.Clients.Client(screenCasterID).SendAsync("GetScreenCast",
                        Context.ConnectionId,
                        RequesterName,
                        AppConfig.RemoteControlNotifyUser,
                        AppConfig.EnforceAttendedAccess,
                        useWebRtc,
                        orgName);
                }
                else
                {
                    await Clients.Caller.SendAsync("Unauthorized");
                }
            }
            else
            {
                SessionInfo.Mode = RemoteControlMode.Normal;
                await Clients.Caller.SendAsync("RequestingScreenCast");
                await CasterHubContext.Clients.Client(screenCasterID).SendAsync("RequestScreenCast", Context.ConnectionId, RequesterName, AppConfig.RemoteControlNotifyUser, AppConfig.UseWebRtc);
            }
        }

    }
}
