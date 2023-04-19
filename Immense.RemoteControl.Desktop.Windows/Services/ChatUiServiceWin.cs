using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.UI.WPF.Services;
using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using Immense.RemoteControl.Desktop.UI.WPF.Views;
using Immense.RemoteControl.Shared.Models;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Immense.RemoteControl.Immense.RemoteControl.Desktop.Windows.Services;

public class ChatUiServiceWin : IChatUiService
{
    private readonly IWindowsUiDispatcher _dispatcher;
    private readonly IShutdownService _shutdownService;
    private readonly IViewModelFactory _viewModelFactory;

    public ChatUiServiceWin(
        IWindowsUiDispatcher dispatcher,
        IShutdownService shutdownService,
        IViewModelFactory viewModelFactory)
    {
        _dispatcher = dispatcher;
        _shutdownService = shutdownService;
        _viewModelFactory = viewModelFactory;
    }

    private ChatWindowViewModel? _chatViewModel;

    public event EventHandler? ChatWindowClosed;

    public Task ReceiveChat(ChatMessage chatMessage)
    {
        _dispatcher.InvokeWpf(() =>
        {
            if (chatMessage.Disconnected)
            {
                // TODO: IDialogService
                System.Windows.MessageBox.Show("Your partner has disconnected.", "Partner Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
                _shutdownService.Shutdown();
                return;
            }

            if (_chatViewModel != null)
            {
                _chatViewModel.SenderName = chatMessage.SenderName;
                _chatViewModel.ChatMessages.Add(chatMessage);
            }
        });
        return Task.CompletedTask;
    }

    public void ShowChatWindow(string organizationName, StreamWriter writer)
    {
        _dispatcher.InvokeWpf(() =>
        {
            _chatViewModel = _viewModelFactory.CreateChatWindowViewModel(organizationName, writer);
            var chatWindow = new ChatWindow();
            chatWindow.Closing += ChatWindow_Closing;
            chatWindow.DataContext = _chatViewModel;
            chatWindow.Show();
        });
    }

    private void ChatWindow_Closing(object? sender, CancelEventArgs e)
    {
        ChatWindowClosed?.Invoke(this, EventArgs.Empty);
    }
}
