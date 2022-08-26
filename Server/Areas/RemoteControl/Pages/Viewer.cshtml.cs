using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.Server.Areas.RemoteControl.Pages
{
    [ServiceFilter(typeof(ViewerFilterAttribute))]
    public class ViewerModel : PageModel
    {
        private readonly IViewerPageDataProvider _viewerDataProvider;

        public ViewerModel(IViewerPageDataProvider viewerDataProvider)
        {
            _viewerDataProvider = viewerDataProvider;
        }

        public string ThemeUrl { get; private set; } = string.Empty;
        public string UserDisplayName { get; private set; } = string.Empty;

        public void OnGet()
        {
            ThemeUrl = _viewerDataProvider.GetThemeUrl(this);
            UserDisplayName = _viewerDataProvider.GetUserDisplayName(this);
        }
    }
}
