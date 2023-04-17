using Avalonia.Threading;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Shared.ViewModels;
using Immense.RemoteControl.Desktop.UI.Controls;
using Immense.RemoteControl.Desktop.UI.Controls.Dialogs;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Desktop.UI.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Linux.Services
{
    public class FileTransferServiceLinux : IFileTransferService
    {
        private static readonly ConcurrentDictionary<string, FileTransferWindow> _fileTransferWindows = new();
        private static readonly ConcurrentDictionary<string, FileStream> _partialTransfers = new();
        private static readonly SemaphoreSlim _writeLock = new(1, 1);
        private static volatile bool _messageBoxPending;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IAvaloniaDispatcher _dispatcher;
        private readonly ILogger<FileTransferServiceLinux> _logger;

        public FileTransferServiceLinux(
            IViewModelFactory viewModelFactory,
            IAvaloniaDispatcher dispatcher,
            ILogger<FileTransferServiceLinux> logger)
        {
            _viewModelFactory = viewModelFactory;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public string GetBaseDirectory()
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (Directory.Exists(desktopDir))
            {
                return desktopDir;
            }

            return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "RemoteControl")).FullName;
        }

        public void OpenFileTransferWindow(IViewer viewer)
        {
            _dispatcher.Post(() =>
            {
                if (_fileTransferWindows.TryGetValue(viewer.ViewerConnectionID, out var window))
                {
                    window.Activate();
                }
                else
                {
                    window = new FileTransferWindow
                    {
                        DataContext = _viewModelFactory.CreateFileTransferWindowViewModel(viewer)
                    };
                    window.Closed += (sender, arg) =>
                    {
                        _fileTransferWindows.Remove(viewer.ViewerConnectionID, out _);
                    };
                    _fileTransferWindows.AddOrUpdate(viewer.ViewerConnectionID, window, (k, v) => window);
                    window.Show();
                }
            });
        }

        public async Task ReceiveFile(byte[] buffer, string fileName, string messageId, bool endOfFile, bool startOfFile)
        {
            try
            {
                await _writeLock.WaitAsync();

                var baseDir = GetBaseDirectory();

                if (startOfFile)
                {
                    var filePath = Path.Combine(baseDir, fileName);

                    if (File.Exists(filePath))
                    {
                        var count = 0;
                        var ext = Path.GetExtension(fileName);
                        var fileWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        while (File.Exists(filePath))
                        {
                            filePath = Path.Combine(baseDir, $"{fileWithoutExt}-{count}{ext}");
                            count++;
                        }
                    }

                    File.Create(filePath).Close();

                    var fs = new FileStream(filePath, FileMode.OpenOrCreate);
                    _partialTransfers.AddOrUpdate(messageId, fs, (k, v) => fs);
                }

                var fileStream = _partialTransfers[messageId];

                if (buffer?.Length > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, buffer.Length);

                }

                if (endOfFile)
                {
                    fileStream.Close();
                    _partialTransfers.Remove(messageId, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while receiving file.");
            }
            finally
            {
                _writeLock.Release();
                if (endOfFile)
                {
                    await Task.Run(ShowTransferComplete);
                }
            }
        }

        public async Task UploadFile(FileUpload fileUpload, IViewer viewer, CancellationToken cancelToken, Action<double> progressUpdateCallback)
        {
            try
            {
                await viewer.SendFile(fileUpload, cancelToken, progressUpdateCallback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading file.");
            }
        }

        private async Task ShowTransferComplete()
        {
            // Prevent multiple dialogs from popping up.
            if (!_messageBoxPending)
            {
                _messageBoxPending = true;

                await MessageBox.Show($"File tranfer complete.  Files saved to directory:\n\n{GetBaseDirectory()}",
                    "Tranfer Complete",
                    MessageBoxType.OK);

                _messageBoxPending = false;
            }
        }
    }
}
