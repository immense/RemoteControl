using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    internal interface IViewModelFactory
    {
        FileTransferWindowViewModel CreateFileTransferWindowViewModel(IViewer viewer);
    }

    internal class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public FileTransferWindowViewModel CreateFileTransferWindowViewModel(
            IViewer viewer)
        {
            var brandingProvider = _serviceProvider.GetRequiredService<IBrandingProvider>();
            var wpfDispatcher = _serviceProvider.GetRequiredService<IWpfDispatcher>();
            var fileTransfer = _serviceProvider.GetRequiredService<IFileTransferService>();
            var logger = _serviceProvider.GetRequiredService<ILogger<FileTransferWindowViewModel>>();
            return new FileTransferWindowViewModel(viewer, brandingProvider, wpfDispatcher, fileTransfer, logger);
        }
    }
}
