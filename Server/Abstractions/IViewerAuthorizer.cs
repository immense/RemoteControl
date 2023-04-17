using Microsoft.AspNetCore.Mvc.Filters;

namespace Immense.RemoteControl.Server.Abstractions;

public interface IViewerAuthorizer
{
    /// <summary>
    /// Example: "/Account/Login"
    /// </summary>
    string UnauthorizedRedirectUrl { get; }
    bool IsAuthorized(AuthorizationFilterContext context);
}
