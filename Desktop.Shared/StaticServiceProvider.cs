using Immense.RemoteControl.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared
{
    public static class StaticServiceProvider
    {
        private static IServiceProvider? _instance;
        public static IServiceProvider Instance
        {
            get
            {
                if (!WaitHelper.WaitFor(() => _instance is not null, TimeSpan.FromSeconds(5)))
                {
                    throw new Exception("ServiceProvider was not created in time.");
                }
                return _instance!;
            }
            set => _instance = value;
        }
    }
}
