using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Native.Win32;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels;

public interface IFileTransferWindowViewModel
{
    ObservableCollection<FileUpload> FileUploads { get; }
    AsyncRelayCommand OpenFileDialogCommand { get; }
    RelayCommand<FileUpload> RemoveFileUploadCommand { get; }
    string ViewerConnectionId { get; set; }
    string ViewerName { get; set; }

    Task OpenFileUploadDialog();
    void RemoveFileUpload(FileUpload? fileUpload);
    Task UploadFile(string filePath);
}

public class FileTransferWindowViewModel : BrandedViewModelBase, IFileTransferWindowViewModel
{
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IFileTransferService _fileTransferService;
    private readonly IViewer _viewer;


    public FileTransferWindowViewModel(
        IViewer viewer,
        IBrandingProvider brandingProvider,
        IWindowsUiDispatcher dispatcher,
        IFileTransferService fileTransferService,
        ILogger<FileTransferWindowViewModel> logger)
        : base(brandingProvider, dispatcher, logger)
    {
        _fileTransferService = fileTransferService;
        _viewer = viewer;
        _dispatcher = dispatcher;
        ViewerName = viewer.Name;
        ViewerConnectionId = viewer.ViewerConnectionID;

        OpenFileDialogCommand = new AsyncRelayCommand(OpenFileUploadDialog);
        RemoveFileUploadCommand = new RelayCommand<FileUpload>(RemoveFileUpload);
    }

    public ObservableCollection<FileUpload> FileUploads { get; } = new();

    public AsyncRelayCommand OpenFileDialogCommand { get; }
    public RelayCommand<FileUpload> RemoveFileUploadCommand { get; }

    public string ViewerConnectionId
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }
    public string ViewerName
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

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


    public void RemoveFileUpload(FileUpload? fileUpload)
    {
        if (fileUpload is null)
        {
            return;
        }

        FileUploads.Remove(fileUpload);
        fileUpload.CancellationTokenSource.Cancel();
    }

    public async Task UploadFile(string filePath)
    {
        var fileUpload = new FileUpload()
        {
            FilePath = filePath
        };

        _dispatcher.InvokeWpf(() =>
        {
            FileUploads.Add(fileUpload);
        });

        await _fileTransferService.UploadFile(fileUpload, _viewer, 
            (double progress) =>
            {
                _dispatcher.InvokeWpf(() => fileUpload.PercentProgress = progress);
            },
            fileUpload.CancellationTokenSource.Token);
    }
}