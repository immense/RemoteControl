using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Immense.RemoteControl.Desktop.ViewModels;

namespace Immense.RemoteControl.Desktop.Views;

public partial class HostNamePrompt : Window
{
    public HostNamePrompt()
    {
        InitializeComponent();
    }

    public HostNamePromptViewModel? ViewModel => DataContext as HostNamePromptViewModel;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
