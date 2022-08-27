using Immense.RemoteControl.Server.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Abstractions
{
    public interface IViewerPageDataProvider
    {
        ViewerPageTheme GetTheme(PageModel pageModel);
        string GetUserDisplayName(PageModel pageModel);
    }
}
