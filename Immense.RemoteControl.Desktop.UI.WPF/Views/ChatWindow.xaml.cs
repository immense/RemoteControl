using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Immense.RemoteControl.Desktop.UI.WPF.Views;

/// <summary>
/// Interaction logic for ChatWindow.xaml
/// </summary>
public partial class ChatWindow : Window
{
    public ChatWindow()
    {
        InitializeComponent();
    }

    public ChatWindow(ChatWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private ChatWindowViewModel? ViewModel => DataContext as ChatWindowViewModel;

    private async void ChatInputBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            await ViewModel.SendChatMessage();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ItemContainerGenerator_ItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs e)
    {
        MessagesScrollViewer.ScrollToEnd();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        Topmost = false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        MessagesItemsControl.ItemContainerGenerator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
