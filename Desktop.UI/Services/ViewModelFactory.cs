using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Immense.RemoteControl.Desktop.UI.Services;

// Normally, I'd use a view model locator.  But enough view models require a factory pattern
// that I thought it more consistent to put them all here.
public interface IViewModelFactory
{
    ChatWindowViewModel CreateChatWindowViewModel(string organizationName, StreamWriter streamWriter);
    FileTransferWindowViewModel CreateFileTransferWindowViewModel(IViewer viewer);
    HostNamePromptViewModel CreateHostNamePromptViewModel();
    PromptForAccessWindowViewModel CreatePromptForAccessViewModel(string requesterName, string organizationName);
}

internal class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ChatWindowViewModel CreateChatWindowViewModel(string organizationName, StreamWriter streamWriter)
    {
        var branding = _serviceProvider.GetRequiredService<IBrandingProvider>();
        var dispatcher = _serviceProvider.GetRequiredService<IAvaloniaDispatcher>();
        var logger = _serviceProvider.GetRequiredService<ILogger<ChatWindowViewModel>>();
        return new ChatWindowViewModel(streamWriter, organizationName, branding, dispatcher, logger);
    }

    public FileTransferWindowViewModel CreateFileTransferWindowViewModel(
        IViewer viewer)
    {
        var brandingProvider = _serviceProvider.GetRequiredService<IBrandingProvider>();
        var dispatcher = _serviceProvider.GetRequiredService<IAvaloniaDispatcher>();
        var logger = _serviceProvider.GetRequiredService<ILogger<FileTransferWindowViewModel>>();
        var fileTransfer = _serviceProvider.GetRequiredService<IFileTransferService>();
        return new FileTransferWindowViewModel(viewer, brandingProvider, dispatcher, fileTransfer, logger);
    }

    public PromptForAccessWindowViewModel CreatePromptForAccessViewModel(string requesterName, string organizationName)
    {
        var brandingProvider = _serviceProvider.GetRequiredService<IBrandingProvider>();
        var dispatcher = _serviceProvider.GetRequiredService<IAvaloniaDispatcher>();
        var logger = _serviceProvider.GetRequiredService<ILogger<PromptForAccessWindowViewModel>>();
        return new PromptForAccessWindowViewModel(requesterName, organizationName, brandingProvider, dispatcher, logger);
    }

    public HostNamePromptViewModel CreateHostNamePromptViewModel()
    {
        var brandingProvider = _serviceProvider.GetRequiredService<IBrandingProvider>();
        var dispatcher = _serviceProvider.GetRequiredService<IAvaloniaDispatcher>();
        var logger = _serviceProvider.GetRequiredService<ILogger<HostNamePromptViewModel>>();
        return new HostNamePromptViewModel(brandingProvider, dispatcher, logger);
    }
}
