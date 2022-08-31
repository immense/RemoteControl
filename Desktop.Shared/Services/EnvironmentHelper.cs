using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Services
{
    public interface IEnvironmentHelper
    {
        bool IsDebug { get; }
    }

    internal class EnvironmentHelper : IEnvironmentHelper
    {
        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }
}
