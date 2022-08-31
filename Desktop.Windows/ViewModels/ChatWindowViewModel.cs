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
    public partial class ChatWindowViewModel : BrandedViewModelBase
    {
        private readonly StreamWriter _streamWriter;
        [ObservableProperty]
        private string _inputText = string.Empty;

        [ObservableProperty]
        private string _organizationName = "your IT provider";

        [ObservableProperty]
        private string _senderName = "a technician";

#nullable disable
        [Obsolete("Parameterless constructor used only for WPF design-time DataContext")]
        public ChatWindowViewModel() { }
#nullable enable

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
