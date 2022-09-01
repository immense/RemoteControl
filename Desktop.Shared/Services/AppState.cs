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

        event EventHandler<IViewer> ViewerAdded;

        event EventHandler<string> ViewerRemoved;

        string AccessKey { get; }
        Dictionary<string, string> ArgDict { get; }
        string Host { get; set; }
        AppMode Mode { get; set; }
        string OrganizationName { get; }
        string PipeName { get; }
        string RequesterName { get; }
        string SessionId { get; }
        ConcurrentDictionary<string, IViewer> Viewers { get; }
        void InvokeScreenCastRequested(ScreenCastRequest viewerIdAndRequesterName);
        void InvokeViewerAdded(IViewer viewer);
        void InvokeViewerRemoved(string viewerID);
        void UpdateHost(string host);
    }

    public class AppState : IAppState
    {
        private readonly ILogger<AppState> _logger;

        public AppState(ILogger<AppState> logger)
        {
            _logger = logger;
            ProcessArgs();
        }

        public event EventHandler<ScreenCastRequest>? ScreenCastRequested;

        public event EventHandler<IViewer>? ViewerAdded;

        public event EventHandler<string>? ViewerRemoved;

        public string AccessKey { get; init; } = string.Empty;
        public Dictionary<string, string> ArgDict { get; } = new();


        public string Host { get; set; } = string.Empty;

        public AppMode Mode { get; set; }

        public string OrganizationName { get; init; } = string.Empty;

        public string PipeName { get; init; } = string.Empty;
        public string RequesterName { get; init; } = string.Empty;
        public string SessionId { get; init; } = string.Empty;
        public ConcurrentDictionary<string, IViewer> Viewers { get; } = new();
        public void InvokeScreenCastRequested(ScreenCastRequest viewerIdAndRequesterName)
        {
            ScreenCastRequested?.Invoke(null, viewerIdAndRequesterName);
        }

        public void InvokeViewerAdded(IViewer viewer)
        {
            ViewerAdded?.Invoke(null, viewer);
        }

        public void InvokeViewerRemoved(string viewerID)
        {
            ViewerRemoved?.Invoke(null, viewerID);
        }

        public void UpdateHost(string host)
        {
            Host = host;
        }

        private void ProcessArgs()
        {
            var args = Environment.GetCommandLineArgs()
                .SkipWhile(x => !x.StartsWith("-"))
                .ToArray();

            for (var i = 0; i < args.Length; i += 2)
            {
                try
                {
                    var key = args[i];
                    if (key != null)
                    {
                        if (!key.Contains("-"))
                        {
                            _logger.LogWarning("Command line arguments are invalid.  Key: {key}", key);
                            i -= 1;
                            continue;
                        }

                        key = key.Trim().TrimStart('-').TrimStart('-').ToLower();

                        ArgDict.Add(key, args[i + 1].Trim());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing args.");
                }

            }
        }
    }
}
