using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes;

public class FakePromptForAccessViewModel : FakeBrandedViewModelBase, IPromptForAccessWindowViewModel
{
    public string OrganizationName { get; set; } = "Test Organization";
    public bool PromptResult { get; set; }
    public string RequesterName { get; set; } = "Test Requester";

    public RelayCommand<Window> SetResultNoCommand { get; } = new(window => { });

    public RelayCommand<Window> SetResultYesCommand { get; } = new(window => { });

    public void SetResultNo(Window? promptWindow)
    {

    }

    public void SetResultYes(Window? promptWindow)
    {

    }
}
