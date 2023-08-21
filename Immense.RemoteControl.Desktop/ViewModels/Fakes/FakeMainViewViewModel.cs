using Immense.RemoteControl.Desktop.Shared.Reactive;
using Immense.RemoteControl.Desktop.Shared.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Immense.RemoteControl.Desktop.ViewModels.Fakes;
public class FakeMainViewViewModel : FakeBrandedViewModelBase, IMainViewViewModel
{

    public AsyncRelayCommand ChangeServerCommand => new(() => Task.CompletedTask);

    public ICommand CloseCommand => new RelayCommand(() => { });
    public AsyncRelayCommand CopyLinkCommand => new(() => Task.CompletedTask);
    public double CopyMessageOpacity { get; set; }

    public string Host { get; set; } = string.Empty;

    public bool IsAdministrator => true;

    public bool IsCopyMessageVisible { get; set; }
    public ICommand MinimizeCommand => new RelayCommand(() => { });
    public ICommand OpenOptionsMenu => new RelayCommand(() => { });
    public AsyncRelayCommand RemoveViewersCommand => new(() => Task.CompletedTask);

    public string StatusMessage { get; set; } = string.Empty;

    public ObservableCollection<IViewer> Viewers { get; } = new();

    public IList<IViewer> SelectedViewers { get; } = new List<IViewer>();

    public bool CanRemoveViewers()
    {
        return true;
    }

    public Task ChangeServer()
    {
        throw new NotImplementedException();
    }

    public Task CopyLink()
    {
        return Task.CompletedTask;
    }

    public Task GetSessionID()
    {
        return Task.CompletedTask;
    }

    public Task Init()
    {
        return Task.CompletedTask;
    }

    public Task PromptForHostName()
    {
        return Task.CompletedTask;
    }

    public Task RemoveViewers()
    {
        return Task.CompletedTask;
    }

}
