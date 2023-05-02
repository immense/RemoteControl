using Avalonia.Controls;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Controls.Dialogs;
using Immense.RemoteControl.Desktop.Services;
using Immense.RemoteControl.Desktop.ViewModels;
using Immense.RemoteControl.Desktop.Views;
using Immense.RemoteControl.Shared.Models;
using System.ComponentModel;
using System;
using System.Threading.Tasks;
using System.IO;

namespace Immense.RemoteControl.Desktop.Services.Linux;

public class ChatUiServiceLinux : IChatUiService
{
    private readonly IAvaloniaDispatcher _dispatcher;
    private readonly IViewModelFactory _viewModelFactory;
    private ChatWindowViewModel? _chatViewModel;

    public ChatUiServiceLinux(
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
                await MessageBox.Show("The partner has disconnected.", "Partner Disconnected", MessageBoxType.OK);
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
