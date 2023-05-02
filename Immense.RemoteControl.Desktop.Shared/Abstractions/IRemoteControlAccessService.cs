namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IRemoteControlAccessService
{
    bool IsPromptOpen { get; }

    Task<bool> PromptForAccess(string requesterName, string organizationName);
}
