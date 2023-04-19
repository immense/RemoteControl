using Avalonia.Controls;
using Immense.RemoteControl.Desktop.Services;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Immense.RemoteControl.Desktop.ViewModels;

public interface IPromptForAccessWindowViewModel
{
    ICommand CloseCommand { get; }
    ICommand MinimizeCommand { get; }
    string OrganizationName { get; set; }
    bool PromptResult { get; set; }
    string RequesterName { get; set; }
    string RequestMessage { get; }
    ICommand SetResultNo { get; }
    ICommand SetResultYes { get; }
}

public class PromptForAccessWindowViewModel : BrandedViewModelBase, IPromptForAccessWindowViewModel
{
    public PromptForAccessWindowViewModel(
        string requesterName,
        string organizationName,
        IBrandingProvider brandingProvider,
        IAvaloniaDispatcher dispatcher,
        ILogger<BrandedViewModelBase> logger)
        : base(brandingProvider, dispatcher, logger)
    {
        if (!string.IsNullOrWhiteSpace(requesterName))
        {
            RequesterName = requesterName;
        }

        if (!string.IsNullOrWhiteSpace(organizationName))
        {
            OrganizationName = organizationName;
        }
    }

    public ICommand CloseCommand { get; } = new RelayCommand<Window>(window =>
    {
        window?.Close();
    });

    public ICommand MinimizeCommand { get; } = new RelayCommand<Window>(window =>
    {
        if (window is not null)
        {
            window.WindowState = WindowState.Minimized;
        }
    });

    public string OrganizationName
    {
        get => Get<string>() ?? "your IT provider";
        set
        {
            Set(value);
            OnPropertyChanged(nameof(RequestMessage));
        }

    }

    public bool PromptResult { get; set; }

    public string RequesterName
    {
        get => Get<string>() ?? "a technician";
        set
        {
            Set(value);
            OnPropertyChanged(nameof(RequestMessage));
        }
    }

    public string RequestMessage
    {
        get
        {
            return $"Would you like to allow {RequesterName} from {OrganizationName} to control your computer?";
        }
    }
    public ICommand SetResultNo => new RelayCommand<Window>(window =>
    {
        PromptResult = false;
        if (window is not null)
        {
            window.Close();
        }
    });

    public ICommand SetResultYes => new RelayCommand<Window>(window =>
    {
        PromptResult = true;
        if (window is not null)
        {
            window.Close();
        }
    });
}
