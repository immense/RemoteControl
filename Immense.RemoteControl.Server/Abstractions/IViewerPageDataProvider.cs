using Immense.RemoteControl.Server.Areas.RemoteControl.Pages;
using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Server.Abstractions;

public interface IViewerPageDataProvider
{
    Task<ViewerPageTheme> GetTheme(PageModel pageModel);
    Task<string> GetUserDisplayName(PageModel pageModel);
    Task<string> GetPageTitle(PageModel pageModel);
    Task<string> GetProductName(PageModel pageModel);
    Task<string> GetProductSubtitle(PageModel pageModel);
    Task<string> GetPageDescription(ViewerModel viewerModel);
}
