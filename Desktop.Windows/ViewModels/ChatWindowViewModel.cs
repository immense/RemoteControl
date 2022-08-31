using CommunityToolkit.Mvvm.ComponentModel;
using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Immense.RemoteControl.Desktop.Windows.Services;
using Immense.RemoteControl.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Windows.ViewModels
{
    public interface IChatWindowViewModel : IBrandedViewModelBase
    {
        ObservableCollection<ChatMessage> ChatMessages { get; }
        string InputText { get; set; }
        string OrganizationName { get; set; }
        string SenderName { get; set; }

        Task SendChatMessage();
    }

    public class ChatWindowViewModel : BrandedViewModelBase, IChatWindowViewModel
    {
        private readonly StreamWriter _streamWriter;

        public ChatWindowViewModel(
            StreamWriter streamWriter,
            string organizationName,
            IBrandingProvider brandingProvider,
            IWpfDispatcher wpfDispatcher,
            ILogger<BrandedViewModelBase> logger)
            : base(brandingProvider, wpfDispatcher, logger)
        {
            _streamWriter = streamWriter;
            if (!string.IsNullOrWhiteSpace(organizationName))
            {
                OrganizationName = organizationName;
            }
        }

        public ObservableCollection<ChatMessage> ChatMessages { get; } = new ObservableCollection<ChatMessage>();

        public string InputText
        {
            get => Get<string>() ?? string.Empty;
            set => Set(value);
        }

        public string OrganizationName
        {
            get => Get<string>() ?? "your IT provider";
            set => Set(value);
        }

        public string SenderName
        {
            get => Get<string>() ?? "a technician";
            set => Set(value);
        }

        public async Task SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText))
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
    }
}
