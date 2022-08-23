using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.AspNetCore.Abstractions
{
    public interface IThemeUrlProvider
    {
        string GetThemeUrl(PageModel pageModel);
    }
}
