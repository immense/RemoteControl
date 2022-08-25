using Immense.RemoteControl.Server.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Immense.RemoteControl.Server.Filters
{
    internal class ViewerFilterAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private readonly IViewerAuthorizer _authorizer;

        public ViewerFilterAttribute(IViewerAuthorizer authorizer)
        {
            _authorizer = authorizer;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_authorizer.IsAuthorized(context))
            {
                return;
            }

            context.Result = new RedirectToPageResult(_authorizer.UnauthorizedRedirectPageName, new { area = _authorizer.UnauthorizedRedirectArea });
        }
    }
}