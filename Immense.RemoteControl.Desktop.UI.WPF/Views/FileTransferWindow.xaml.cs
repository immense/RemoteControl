using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using System.Windows;

namespace Immense.RemoteControl.Desktop.UI.WPF.Views;

/// <summary>
/// Interaction logic for FileTransferWindow.xaml
/// </summary>
public partial class FileTransferWindow : Window
{

    public FileTransferWindow()
    {
        InitializeComponent();
    }

    public FileTransferWindow(FileTransferWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        if (Screen.PrimaryScreen is not null)
        {
            Left = Screen.PrimaryScreen.WorkingArea.Right - Width;
            Top = Screen.PrimaryScreen.WorkingArea.Bottom - Height;
        }

    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        Topmost = false;
    }
}
