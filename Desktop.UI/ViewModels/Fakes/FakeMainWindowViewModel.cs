using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Immense.RemoteControl.Desktop.UI.ViewModels.Fakes
{
    public class FakeMainWindowViewModel : FakeBrandedViewModelBase, IMainWindowViewModel
    {
        public ICommand ChangeServerCommand { get; } = new RelayCommand(() => { });

        public ICommand CloseCommand => new RelayCommand(() => { });
        public ICommand CopyLinkCommand => new RelayCommand(() => { });
        public double CopyMessageOpacity { get; set; }

        public string Host { get; set; } = string.Empty;

        public bool IsAdministrator => true;

        public bool IsCopyMessageVisible { get; set; }
        public ICommand MinimizeCommand => new RelayCommand(() => { });
        public ICommand OpenOptionsMenu => new RelayCommand(() => { });
        public ICommand RemoveViewersCommand { get; } = new RelayCommand(() => { });

        public string StatusMessage { get; set; } = string.Empty;

        public ObservableCollection<IViewer> Viewers { get; } = new();
        public bool CanRemoveViewers(IList<object>? items)
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

        public Task RemoveViewers(AvaloniaList<object>? list)
        {
            return Task.CompletedTask;
        }

    }
}
