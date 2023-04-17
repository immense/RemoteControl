namespace Immense.RemoteControl.Desktop.Shared.Services;

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
