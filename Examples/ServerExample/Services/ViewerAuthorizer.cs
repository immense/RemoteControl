using Immense.RemoteControl.Server.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServerExample.Services
{
    internal class ViewerAuthorizer : IViewerAuthorizer
    {
        public string UnauthorizedRedirectUrl => "/Error";

        public bool IsAuthorized(AuthorizationFilterContext context)
        {
            return true;
        }
    }
}