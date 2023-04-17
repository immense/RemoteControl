using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Server.Abstractions;

public interface IViewerPageDataProvider
{
    ViewerPageTheme GetTheme(PageModel pageModel);
    string GetUserDisplayName(PageModel pageModel);
}
