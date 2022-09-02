using Avalonia.Controls;
using Immense.RemoteControl.Desktop.UI.Services;
using Immense.RemoteControl.Shared.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using System;

namespace Immense.RemoteControl.Desktop.UI.ViewModels
{
    public interface IChatWindowViewModel
    {
        ObservableCollection<ChatMessage> ChatMessages { get; }
        string ChatSessionHeader { get; }
        ICommand CloseCommand { get; }
        string InputText { get; set; }
        ICommand MinimizeCommand { get; }
        string OrganizationName { get; set; }
        string SenderName { get; set; }

        Task SendChatMessage();
    }

    public class ChatWindowViewModel : BrandedViewModelBase, IChatWindowViewModel
    {
        private readonly StreamWriter? _streamWriter;

        public ChatWindowViewModel(
                    StreamWriter streamWriter,
            string organizationName,
            IBrandingProvider brandingProvider,
            IAvaloniaDispatcher dispatcher,
            ILogger<ChatWindowViewModel> logger)
            : base(brandingProvider, dispatcher, logger)
        {
            _streamWriter = streamWriter;
            if (!string.IsNullOrWhiteSpace(organizationName))
            {
                OrganizationName = organizationName;
            }
            CloseCommand = new RelayCommand<Window>(CloseWindow);
            MinimizeCommand = new RelayCommand<Window>(MinimizeWindow);
        }


        public ObservableCollection<ChatMessage> ChatMessages { get; } = new ObservableCollection<ChatMessage>();

        public string ChatSessionHeader => $"Chat session with {OrganizationName}";

        public ICommand CloseCommand { get; }

        public string InputText
        {
            get => Get<string>() ?? string.Empty;
            set => Set(value);
        }

        public ICommand MinimizeCommand { get; }

        public string OrganizationName
        {
            get => Get<string>() ?? "your IT provider";
            set
            {
                Set(value);
                OnPropertyChanged(nameof(ChatSessionHeader));
            }
        }

        public string SenderName
        {
            get => Get<string>() ?? "a technician";
            set => Set(value);
        }

        public async Task SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText) ||
                _streamWriter is null)
            {
                return;
            }

            var chatMessage = new ChatMessage(string.Empty, InputText);
            InputText = string.Empty;
            await _streamWriter.WriteLineAsync(JsonSerializer.Serialize(chatMessage));
            await _streamWriter.FlushAsync();
            chatMessage.SenderName = "You";
            ChatMessages.Add(chatMessage);
        }

        private void CloseWindow(Window? obj)
        {
            obj?.Close();
        }

        private void MinimizeWindow(Window? obj)
        {
            if (obj is not null)
            {
                obj.WindowState = WindowState.Minimized;
            }
        }
    }
}
