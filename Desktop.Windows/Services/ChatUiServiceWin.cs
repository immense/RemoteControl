using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Windows.ViewModels;
using Immense.RemoteControl.Shared.Models;
using Remotely.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Immense.RemoteControl.Desktop.Windows.Services
{
    public class ChatUiServiceWin : IChatUiService
    {
        private readonly IWpfDispatcher _wpfDispatcher;
        private readonly IShutdownService _shutdownService;

        public ChatUiServiceWin(IWpfDispatcher wpfDispatcher, IShutdownService shutdownService)
        {
            _wpfDispatcher = wpfDispatcher;
            _shutdownService = shutdownService;
        }

        private ChatWindowViewModel? _chatViewModel;

        public event EventHandler? ChatWindowClosed;

        public void ReceiveChat(ChatMessage chatMessage)
        {
            _wpfDispatcher.Invoke(() =>
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
        }

        public void ShowChatWindow(string organizationName, StreamWriter writer)
        {
            _wpfDispatcher.Invoke(() =>
            {
                var chatWindow = new ChatWindow();
                chatWindow.Closing += ChatWindow_Closing;
                _chatViewModel = chatWindow.DataContext as ChatWindowViewModel;
                _chatViewModel.PipeStreamWriter = writer;
                _chatViewModel.OrganizationName = organizationName;
                chatWindow.Show();
            });
        }

        private void ChatWindow_Closing(object sender, CancelEventArgs e)
        {
            ChatWindowClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
