using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Abstractions
{
    public interface IShutdownService
    {
        Task Shutdown();
    }
}
