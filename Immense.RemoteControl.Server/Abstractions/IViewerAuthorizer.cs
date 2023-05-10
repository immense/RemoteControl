using Microsoft.AspNetCore.Mvc.Filters;

namespace Immense.RemoteControl.Server.Abstractions;

public interface IViewerAuthorizer
{
    /// <summary>
    /// Where the browser should be redirected if IsAuthorized returns false.
    /// Example: "/Account/Login"
    /// </summary>
    string UnauthorizedRedirectUrl { get; }

    /// <summary>
    /// Whether the current user is authorized to view the remote control page.
    /// Note: This does not inherently give access to any devices or resources.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task<bool> IsAuthorized(AuthorizationFilterContext context);
}
