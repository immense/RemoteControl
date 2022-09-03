using Immense.RemoteControl.Desktop.UI.WPF.ViewModels;
using Immense.RemoteControl.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.UI.WPF.ViewModels.Fakes
{
    public class FakeChatWindowViewModel : FakeBrandedViewModelBase, IChatWindowViewModel
    {
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new()
        {
            new ChatMessage("Designer", "This is a design-time test message.")
        };

        public string InputText
        {
            get => "Some text I'm going to send.";
            set { }
        }
        public string OrganizationName
        {
            get => "Design-Time Technicians";
            set { }
        }
        public string SenderName
        {
            get => "Test Tech";
            set { }
        }

        public Task SendChatMessage()
        {
            return Task.CompletedTask;
        }
    }
}
