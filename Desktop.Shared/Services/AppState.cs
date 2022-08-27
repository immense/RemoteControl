using Immense.RemoteControl.Desktop.Shared.Enums;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IAppState
    {
        event EventHandler<ScreenCastRequest> ScreenCastRequested;

        event EventHandler<Viewer> ViewerAdded;

        event EventHandler<string> ViewerRemoved;

        Dictionary<string, string> ArgDict { get; }
        string DeviceID { get; }
        string Host { get; }
        AppMode Mode { get; }
        string OrganizationId { get; }
        string OrganizationName { get; }
        string RequesterID { get; }
        string ServiceID { get; }
        ConcurrentDictionary<string, Viewer> Viewers { get; }
        void InvokeScreenCastRequested(ScreenCastRequest viewerIdAndRequesterName);
        void InvokeViewerAdded(Viewer viewer);
        void InvokeViewerRemoved(string viewerID);
        void ProcessArgs(string[] args);
        void UpdateHost(string host);
        void UpdateOrganizationId(string organizationId);
    }

    public class AppState : IAppState
    {
        private readonly ILogger<AppState> _logger;

        public AppState(ILogger<AppState> logger)
        {
            _logger = logger;
        }

        public event EventHandler<ScreenCastRequest>? ScreenCastRequested;

        public event EventHandler<Viewer>? ViewerAdded;

        public event EventHandler<string>? ViewerRemoved;

        public Dictionary<string, string> ArgDict { get; } = new();
        public string DeviceID { get; private set; } = string.Empty;
        public string Host { get; private set; } = string.Empty;
        public AppMode Mode { get; private set; }
        public string OrganizationId { get; private set; } = string.Empty;
        public string OrganizationName { get; private set; } = string.Empty;
        public string RequesterID { get; private set; } = string.Empty;
        public string ServiceID { get; private set; } = string.Empty;

        public ConcurrentDictionary<string, Viewer> Viewers { get; } = new();

        public void InvokeScreenCastRequested(ScreenCastRequest viewerIdAndRequesterName)
        {
            ScreenCastRequested?.Invoke(null, viewerIdAndRequesterName);
        }

        public void InvokeViewerAdded(Viewer viewer)
        {
            ViewerAdded?.Invoke(null, viewer);
        }

        public void InvokeViewerRemoved(string viewerID)
        {
            ViewerRemoved?.Invoke(null, viewerID);
        }

        public void ProcessArgs(string[] args)
        {
            ArgDict.Clear();
            for (var i = 0; i < args.Length; i += 2)
            {
                try
                {
                    var key = args[i];
                    if (key != null)
                    {
                        if (!key.Contains('-'))
                        {
                            _logger.LogWarning("Command line arguments are invalid.  Key: {key}", key);
                            i -= 1;
                            continue;
                        }

                        key = key.Trim().Replace("-", "").ToLower();

                        ArgDict.Add(key, args[i + 1].Trim());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing args.");
                }

            }

            if (ArgDict.TryGetValue("mode", out var mode))
            {
                Mode = (AppMode)Enum.Parse(typeof(AppMode), mode, true);
            }
            else
            {
                Mode = AppMode.Attended;
            }

            if (ArgDict.TryGetValue("host", out var host))
            {
                Host = host;
            }
            if (ArgDict.TryGetValue("requester", out var requester))
            {
                RequesterID = requester;
            }
            if (ArgDict.TryGetValue("serviceid", out var serviceID))
            {
                ServiceID = serviceID;
            }
            if (ArgDict.TryGetValue("deviceid", out var deviceID))
            {
                DeviceID = deviceID;
            }
            if (ArgDict.TryGetValue("organization", out var orgName))
            {
                OrganizationName = orgName;
            }
            if (ArgDict.TryGetValue("orgid", out var orgId))
            {
                OrganizationId = orgId;
            }
        }

        public void UpdateHost(string host)
        {
            Host = host;
        }

        public void UpdateOrganizationId(string organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}
