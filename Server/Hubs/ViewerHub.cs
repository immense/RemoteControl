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
    [ServiceFilter(typeof(ViewerAuthorizationFilter))]
    public class ViewerHub : Hub
    {
        private readonly IHubEventHandler _hubEvents;
        private readonly IDesktopHubSessionCache _desktopSessionCache;
        private readonly IServiceHubSessionCache _serviceSessionCache;
        private readonly IViewerHubDataProvider _viewerHubDataProvider;
        private readonly IHubContext<DesktopHub> _desktopHub;
        private readonly ILogger<ViewerHub> _logger;

        public ViewerHub(
            IHubEventHandler hubEvents,
            IDesktopHubSessionCache desktopSessionCache,
            IServiceHubSessionCache serviceSessionCache,
            IViewerHubDataProvider viewerHubDataProvider,
            IHubContext<DesktopHub> desktopHub,
            ILogger<ViewerHub> logger)
        {
            _hubEvents = hubEvents;
            _desktopSessionCache = desktopSessionCache;
            _serviceSessionCache = serviceSessionCache;
            _viewerHubDataProvider = viewerHubDataProvider;
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
                if (!_desktopSessionCache.Sessions.Values.Any(x => x.AttendedSessionID == screenCasterID))
                {
                    await Clients.Caller.SendAsync("SessionIDNotFound");
                    return;
                }

                screenCasterID = _desktopSessionCache.Sessions.Values.First(x => x.AttendedSessionID == screenCasterID).CasterConnectionId;
            }

            if (!_desktopSessionCache.Sessions.TryGetValue(screenCasterID, out var sessionInfo))
            {
                await Clients.Caller.SendAsync("SessionIDNotFound");
                return;
            }

            SessionInfo = sessionInfo;
            ScreenCasterID = screenCasterID;
            RequesterName = requesterName;
            Mode = (RemoteControlMode)remoteControlMode;

            string orgId = string.Empty;

            if (Context.User?.Identity?.IsAuthenticated == true)
            {

                if (!string.IsNullOrWhiteSpace(Context.UserIdentifier))
                {
                    RequesterName = _viewerHubDataProvider.GetRequesterDisplayName(Context.UserIdentifier);
                }

                orgId = _viewerHubDataProvider.GetRequesterOrganizationId(Context.UserIdentifier);

                var currentUsers = _desktopSessionCache.Sessions.Values.Count(x =>
                    x.CasterConnectionId != screenCasterID &&
                    x.OrganizationID == orgId &&
                    x.ViewerList.Any());

                var sessionLimit = _viewerHubDataProvider.GetConcurrentSessionLimit();
                if (currentUsers >= sessionLimit)
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
                                $"Login ID (if logged in): {Context.User?.Identity?.Name}.  " +
                                $"Machine Name: {SessionInfo.MachineName}.  " +
                                $"Requester Name (if specified): {RequesterName}.  " +
                                $"Connection ID: {Context.ConnectionId}. User ID: {Context.UserIdentifier}.  " +
                                $"Screen Caster ID: {screenCasterID}.  " +
                                $"Mode: {(RemoteControlMode)remoteControlMode}.  " +
                                $"Requester IP Address: {Context.GetHttpContext()?.Connection?.RemoteIpAddress}";

            _hubEvents.LogRemoteControlStarted(logMessage, orgId);

            if (Mode == RemoteControlMode.Unattended)
            {
                if (!_serviceSessionCache.Sessions.TryGetValue(SessionInfo.ServiceID, out var targetDeviceId))
                {
                    _logger.LogError("Target service ID (id) not found in cache.", SessionInfo.ServiceID);
                    return;
                }

                SessionInfo.Mode = RemoteControlMode.Unattended;

                if ((!string.IsNullOrWhiteSpace(otp) && _viewerHubDataProvider.OtpMatchesDevice(otp, targetDeviceId))
                    ||
                    (Context.User?.Identity?.IsAuthenticated == true && _viewerHubDataProvider.DoesUserHaveAccessToDevice(targetDeviceId, Context.UserIdentifier)))
                {
                    var orgName = _viewerHubDataProvider.GetOrganizationNameById(orgId);
                    await _desktopHub.Clients.Client(screenCasterID).SendAsync("GetScreenCast",
                        Context.ConnectionId,
                        RequesterName,
                        _viewerHubDataProvider.RemoteControlNotifyUser,
                        _viewerHubDataProvider.EnforceAttendedAccess,
                        orgName);
                }
                else
                {
                    await Clients.Caller.SendAsync("Unauthorized");
                }
            }
            else
            {
                SessionInfo.Mode = RemoteControlMode.Attended;
                await Clients.Caller.SendAsync("RequestingScreenCast");
                await _desktopHub.Clients.Client(screenCasterID).SendAsync("RequestScreenCast", Context.ConnectionId, RequesterName, _viewerHubDataProvider.RemoteControlNotifyUser);
            }
        }

    }
}
