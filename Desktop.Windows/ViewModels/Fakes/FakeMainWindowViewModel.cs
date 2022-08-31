using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels.Fakes
{
    public class FakeMainWindowViewModel : FakeBrandedViewModelBase, IMainWindowViewModel
    {
        public bool CanElevateToAdmin => true;

        public bool CanElevateToService => true;

        public AsyncRelayCommand ChangeServerCommand { get; } = new(() => Task.CompletedTask);

        public RelayCommand ElevateToAdminCommand { get; } = new(() => { });

        public RelayCommand ElevateToServiceCommand { get; } = new(() => { });

        public string Host { get; set; } = string.Empty;

        public bool IsAdministrator => true;

        public AsyncRelayCommand<IList<object>> RemoveViewersCommand { get; } = new AsyncRelayCommand<IList<object>>(list => Task.CompletedTask);

        public string SessionId { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;

        public ObservableCollection<IViewer> Viewers { get; } = new();

        public bool CanRemoveViewers(IList<object>? items)
        {
            return true;
        }

        public void CopyLink()
        {
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public void ShutdownApp()
        {
            
        }
    }
}
