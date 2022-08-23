using Immense.RemoteControl.AspNetCore.Abstractions;
using Immense.RemoteControl.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Immense.RemoteControl.AspNetCore.Areas.RemoteControl.Pages
{
    [ServiceFilter(typeof(ViewerFilterAttribute))]
    public class ViewerModel : PageModel
    {
        private readonly IThemeUrlProvider _themeUrlProvider;

        public ViewerModel(IThemeUrlProvider themeUrlProvider)
        {
            _themeUrlProvider = themeUrlProvider;
        }

        public string ThemeUrl { get; private set; } = string.Empty;

        public void OnGet()
        {
            ThemeUrl = _themeUrlProvider.GetThemeUrl(this);
        }
    }
}
