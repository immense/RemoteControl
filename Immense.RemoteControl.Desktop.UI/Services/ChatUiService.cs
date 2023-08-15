using Avalonia.Controls;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Shared.Models;
using System.ComponentModel;
using Immense.RemoteControl.Desktop.UI.Controls.Dialogs;

namespace Immense.RemoteControl.Desktop.UI.Services;

public class ChatUiService : IChatUiService
{
    private readonly IAvaloniaDispatcher _dispatcher;
    private readonly IViewModelFactory _viewModelFactory;
    private ChatWindowViewModel? _chatViewModel;

    public ChatUiService(
        IAvaloniaDispatcher dispatcher,
        IViewModelFactory viewModelFactory)
    {
        _dispatcher = dispatcher;
        _viewModelFactory = viewModelFactory;
    }

    public event EventHandler? ChatWindowClosed;

    public async Task ReceiveChat(ChatMessage chatMessage)
    {
        await _dispatcher.InvokeAsync(async () =>
        {
            if (chatMessage.Disconnected)
            {
                // TODO: IDialogService.
                await MessageBox.Show("The partner has disconnected from the chat.", "Partner Disconnected", MessageBoxType.OK);
                Environment.Exit(0);
                return;
            }

            if (_chatViewModel != null)
            {
                _chatViewModel.SenderName = chatMessage.SenderName;
                _chatViewModel.ChatMessages.Add(chatMessage);
            }
        });
    }

    public void ShowChatWindow(string organizationName, StreamWriter writer)
    {
        if (_dispatcher.CurrentApp is null)
        {
            return;
        }

        _dispatcher.Post(() =>
        {
            _chatViewModel = _viewModelFactory.CreateChatWindowViewModel(organizationName, writer);
            var chatWindow = new ChatWindow()
            {
                DataContext = _chatViewModel
            };

            chatWindow.Closing += ChatWindow_Closing;
            _dispatcher.CurrentApp.Run(chatWindow);
        });
    }

    private void ChatWindow_Closing(object? sender, CancelEventArgs e)
    {
        ChatWindowClosed?.Invoke(this, e);
    }
}
