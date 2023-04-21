using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Filters;
using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Server.Areas.RemoteControl.Pages;

[ServiceFilter(typeof(ViewerAuthorizationFilter))]
public class ViewerModel : PageModel
{
    private readonly IViewerPageDataProvider _viewerDataProvider;

    public ViewerModel(IViewerPageDataProvider viewerDataProvider)
    {
        _viewerDataProvider = viewerDataProvider;
    }

    public string ThemeUrl { get; private set; } = string.Empty;
    public string UserDisplayName { get; private set; } = string.Empty;
    public string PageTitle { get; private set; } = string.Empty;
    public string PageDescription { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string ProductSubtitle { get; private set; } = string.Empty;

    public async Task OnGet()
    {
        var theme = await _viewerDataProvider.GetTheme(this);

        ThemeUrl = theme switch
        {
            ViewerPageTheme.Dark => "/_content/Immense.RemoteControl.Server/css/remote-control-dark.css",
            ViewerPageTheme.Light => "/_content/Immense.RemoteControl.Server/css/remote-control-light.css",
            _ => "/_content/Immense.RemoteControl.Server/css/remote-control-dark.css"
        };
        UserDisplayName = await _viewerDataProvider.GetUserDisplayName(this);
        PageTitle = await _viewerDataProvider.GetPageTitle(this);
        ProductName = await _viewerDataProvider.GetProductName(this);
        ProductSubtitle = await _viewerDataProvider.GetProductSubtitle(this);
        PageDescription = await _viewerDataProvider.GetPageDescription(this);
    }
}
