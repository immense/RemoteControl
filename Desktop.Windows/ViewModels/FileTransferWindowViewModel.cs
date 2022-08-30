using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using Immense.RemoteControl.Desktop.Shared.Win32;
using Immense.RemoteControl.Desktop.Windows.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels
{

    public partial class FileTransferWindowViewModel : BrandedViewModelBase
    {
        private readonly IWpfDispatcher _dispatcher;
        private readonly IFileTransferService _fileTransferService;
        private readonly IViewer _viewer;
        [ObservableProperty]
        private string _viewerConnectionId = string.Empty;

        [ObservableProperty]
        private string _viewerName = string.Empty;

        public FileTransferWindowViewModel() { }
        public FileTransferWindowViewModel(
            IViewer viewer,
            IBrandingProvider brandingProvider,
            IWpfDispatcher wpfDispatcher,
            IFileTransferService fileTransferService,
            ILogger<FileTransferWindowViewModel> logger)
            : base(brandingProvider, wpfDispatcher, logger)
        {
            _fileTransferService = fileTransferService;
            _viewer = viewer;
            _dispatcher = wpfDispatcher;
            _viewerName = viewer.Name;
            ViewerConnectionId = viewer.ViewerConnectionID;
        }

        public ObservableCollection<FileUpload> FileUploads { get; } = new ObservableCollection<FileUpload>();

        [RelayCommand]
        public async Task OpenFileUploadDialog()
        {
            // Change initial directory so it doesn't open in %userprofile% path
            // for SYSTEM account.
            var rootDir = Path.GetPathRoot(Environment.SystemDirectory);
            var userDir = Path.Combine(rootDir!,
                "Users",
                Win32Interop.GetUsernameFromSessionId((uint)Process.GetCurrentProcess().SessionId));

            var ofd = new OpenFileDialog()
            {
                Title = "Upload File via Remotely",
                Multiselect = true,
                CheckFileExists = true,
                InitialDirectory = Directory.Exists(userDir) ? userDir : rootDir
            };

            try
            {
                // The OpenFileDialog throws an error if SYSTEM doesn't have a Desktop folder.
                var desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
                Directory.CreateDirectory(desktop);
            }
            catch { }

            var result = ofd.ShowDialog();
            if (result == DialogResult.Cancel)
            {
                return;
            }
            foreach (var file in ofd.FileNames)
            {
                if (File.Exists(file))
                {
                    await UploadFile(file);
                }
            }
        }

        [RelayCommand]
        public void RemoveFileUpload(FileUpload fileUpload)
        {
            FileUploads.Remove(fileUpload);
            fileUpload.CancellationTokenSource.Cancel();
        }

        public async Task UploadFile(string filePath)
        {
            var fileUpload = new FileUpload()
            {
                FilePath = filePath
            };

            _dispatcher.Invoke(() =>
            {
                FileUploads.Add(fileUpload);
            });

            await _fileTransferService.UploadFile(fileUpload, _viewer, fileUpload.CancellationTokenSource.Token, (double progress) =>
            {
                _dispatcher.Invoke(() => fileUpload.PercentProgress = progress);
            });
        }
    }
}