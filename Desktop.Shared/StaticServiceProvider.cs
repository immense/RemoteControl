using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared
{
    public static class StaticServiceProvider
    {
        public static IServiceProvider? Instance { get; internal set; }
    }
}
