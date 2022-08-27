using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions
{
    public interface IChatClientService
    {
        Task StartChat(string requesterID, string organizationName);
    }
}
