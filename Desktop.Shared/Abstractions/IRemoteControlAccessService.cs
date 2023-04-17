namespace Immense.RemoteControl.Desktop.Shared.Abstractions;

public interface IRemoteControlAccessService
{
    Task<bool> PromptForAccess(string requesterName, string organizationName);
}
