using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes
{
    public class FakeFileTransferWindowViewModel : FakeBrandedViewModelBase, IFileTransferWindowViewModel
    {
        public ObservableCollection<FileUpload> FileUploads { get; } = new();

        public AsyncRelayCommand OpenFileDialogCommand { get; } = new(() => Task.CompletedTask);

        public RelayCommand<FileUpload> RemoveFileUploadCommand { get; } = new RelayCommand<FileUpload>(x => { });

        public string ViewerConnectionId { get; set; } = string.Empty;
        public string ViewerName { get; set; } = string.Empty;

        public Task OpenFileUploadDialog()
        {
            return Task.CompletedTask;
        }

        public void RemoveFileUpload(FileUpload? fileUpload)
        {

        }

        public Task UploadFile(string filePath)
        {
            return Task.CompletedTask;
        }
    }
}
