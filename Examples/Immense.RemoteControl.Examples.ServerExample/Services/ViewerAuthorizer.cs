using Immense.RemoteControl.Server.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Immense.RemoteControl.Examples.ServerExample.Services;

internal class ViewerAuthorizer : IViewerAuthorizer
{
    public string UnauthorizedRedirectUrl => "/Error";

    public Task<bool> IsAuthorized(AuthorizationFilterContext context)
    {
        return Task.FromResult(true);
    }
}