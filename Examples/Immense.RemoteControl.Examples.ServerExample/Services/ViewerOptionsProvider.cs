using Immense.RemoteControl.Examples.ServerExample.Options;
using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Immense.RemoteControl.Examples.ServerExample.Services;

public class ViewerOptionsProvider : IViewerOptionsProvider
{
    private readonly IOptionsMonitor<AppSettingsOptions> _appSettings;

    public ViewerOptionsProvider(IOptionsMonitor<AppSettingsOptions> appSettings)
    {
        _appSettings = appSettings;
    }

    public Task<RemoteControlViewerOptions> GetViewerOptions()
    {
        var options = new RemoteControlViewerOptions()
        {
            ShouldRecordSession = _appSettings.CurrentValue.ShouldRecordSessions
        };

        return Task.FromResult(options);
    }
}
