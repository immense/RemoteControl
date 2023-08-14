using Avalonia;
using Avalonia.Controls;

namespace Immense.RemoteControl.Desktop.UI.Views;

public partial class FileTransferWindow : Window
{
    public FileTransferWindow()
    {
        InitializeComponent();
        Opened += FileTransferWindow_Opened;
    }

    public IFileTransferWindowViewModel? ViewModel => DataContext as IFileTransferWindowViewModel;

    private void FileTransferWindow_Opened(object? sender, EventArgs e)
    {
        Topmost = false;

        if (Screens.Primary is not null)
        {
            var left = Screens.Primary.WorkingArea.Right - Width;
            var top = Screens.Primary.WorkingArea.Bottom - Height;
            Position = new PixelPoint((int)left, (int)top);
        }
    }
}
