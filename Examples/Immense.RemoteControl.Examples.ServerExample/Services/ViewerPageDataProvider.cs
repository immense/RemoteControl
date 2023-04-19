using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Examples.ServerExample.Services;

internal class ViewerPageDataProvider : IViewerPageDataProvider
{
    public ViewerPageTheme GetTheme(PageModel pageModel)
    {
        return ViewerPageTheme.Dark;
    }

    public string GetUserDisplayName(PageModel pageModel)
    {
        return "Han Solo";
    }
}