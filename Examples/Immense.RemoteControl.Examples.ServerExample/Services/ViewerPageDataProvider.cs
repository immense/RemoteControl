using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Areas.RemoteControl.Pages;
using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Examples.ServerExample.Services;

internal class ViewerPageDataProvider : IViewerPageDataProvider
{
    public Task<string> GetFaviconUrl(PageModel pageModel)
    {
        return Task.FromResult("/favicon.ico");
    }

    public Task<string> GetLogoUrl(PageModel pageModel)
    {
        return Task.FromResult("/viewer-logo.svg");
    }

    public Task<string> GetPageDescription(PageModel pageModel)
    {
        return Task.FromResult("Open-source remote support tools.");
    }

    public Task<string> GetPageTitle(PageModel pageModel)
    {
        return Task.FromResult("Remotely Remote Control");
    }

    public Task<string> GetProductName(PageModel pageModel)
    {
        return Task.FromResult("Remotely");
    }

    public Task<string> GetProductSubtitle(PageModel pageModel)
    {
        return Task.FromResult("Remote Control");
    }

    public Task<ViewerPageTheme> GetTheme(PageModel pageModel)
    {
        return Task.FromResult(ViewerPageTheme.Dark);
    }

    public Task<string> GetUserDisplayName(PageModel pageModel)
    {
        return Task.FromResult("Han Solo");
    }
}
