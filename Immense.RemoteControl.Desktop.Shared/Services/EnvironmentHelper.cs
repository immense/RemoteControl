using Immense.RemoteControl.Desktop.Shared.Native.Linux;
using System.Security.Principal;

namespace Immense.RemoteControl.Desktop.Shared.Services;

public interface IEnvironmentHelper
{
    bool IsDebug { get; }
    bool IsElevated { get; }
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

    public bool IsElevated
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            if (OperatingSystem.IsLinux())
            {
                return Libc.geteuid() == 0;
            }
            return false;
        }
    }
}
