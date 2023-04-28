using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;

namespace Immense.RemoteControl.Desktop.UI.WPF.Views;

/// <summary>
/// Interaction logic for PromptForAccessWindow.xaml
/// </summary>
public partial class PromptForAccessWindow : Window
{
    public PromptForAccessWindow()
    {
        InitializeComponent();
    }

    public PromptForAccessWindow(PromptForAccessWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    public PromptForAccessWindowViewModel? ViewModel => DataContext as PromptForAccessWindowViewModel;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        Topmost = false;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
