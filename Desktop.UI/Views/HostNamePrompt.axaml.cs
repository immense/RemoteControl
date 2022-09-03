using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Immense.RemoteControl.Desktop.Shared;
using Immense.RemoteControl.Desktop.UI.ViewModels;
using Immense.RemoteControl.Desktop.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Immense.RemoteControl.Desktop.UI.Views
{
    public partial class HostNamePrompt : Window
    {
        public HostNamePrompt()
        {
            InitializeComponent();
        }

        public HostNamePromptViewModel? ViewModel => DataContext as HostNamePromptViewModel;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
