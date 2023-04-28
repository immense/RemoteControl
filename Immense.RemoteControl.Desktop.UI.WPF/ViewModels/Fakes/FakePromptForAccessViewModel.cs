using Immense.RemoteControl.Desktop.Shared.Reactive;
using System.Windows;
using System.Windows.Input;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes;

public class FakePromptForAccessViewModel : FakeBrandedViewModelBase, IPromptForAccessWindowViewModel
{
    public string OrganizationName { get; set; } = "Test Organization";
    public bool PromptResult { get; set; }
    public string RequesterName { get; set; } = "Test Requester";

    public ICommand SetResultNoCommand { get; } = new RelayCommand<Window>(window => { });

    public ICommand SetResultYesCommand { get; } = new RelayCommand<Window>(window => { });

    public void SetResultNo(Window? promptWindow)
    {

    }

    public void SetResultYes(Window? promptWindow)
    {

    }
}
