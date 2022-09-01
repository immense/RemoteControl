using Immense.RemoteControl.Server.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServerExample.Services
{
    internal class ViewerAuthorizer : IViewerAuthorizer
    {
        public string UnauthorizedRedirectPageName => "Error";

        public string? UnauthorizedRedirectArea => string.Empty;

        public bool IsAuthorized(AuthorizationFilterContext context)
        {
            return true;
        }
    }
}