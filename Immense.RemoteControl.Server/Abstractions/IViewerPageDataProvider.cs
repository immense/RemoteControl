using Immense.RemoteControl.Server.Areas.RemoteControl.Pages;
using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Server.Abstractions;

public interface IViewerPageDataProvider
{
    Task<ViewerPageTheme> GetTheme(PageModel pageModel);
    Task<string> GetUserDisplayName(PageModel pageModel);
    Task<string> GetPageTitle(PageModel pageModel);
    Task<string> GetPageDescription(PageModel viewerModel);
    Task<string> GetFaviconUrl(PageModel viewerModel);
    Task<string> GetLogoUrl(PageModel viewerModel);
}
