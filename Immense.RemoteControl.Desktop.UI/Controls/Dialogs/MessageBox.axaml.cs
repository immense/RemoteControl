using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Diagnostics;
using Immense.RemoteControl.Desktop.Shared;
using Immense.RemoteControl.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Immense.RemoteControl.Desktop.UI.Controls.Dialogs;

public partial class MessageBox : Window
{
    public static async Task<MessageBoxResult> Show(string message, string caption, MessageBoxType type)
    {
        Guard.IsNotNull(StaticServiceProvider.Instance, nameof(StaticServiceProvider.Instance));

        var viewModel = StaticServiceProvider.Instance.GetRequiredService<IMessageBoxViewModel>();
        var messageBox = new MessageBox()
        {
            DataContext = viewModel
        };
        viewModel.Caption = caption;
        viewModel.Message = message;

        switch (type)
        {
            case MessageBoxType.OK:
                viewModel.IsOkButtonVisible = true;
                break;
            case MessageBoxType.YesNo:
                viewModel.AreYesNoButtonsVisible = true;
                break;
            default:
                break;
        }

        var dispatcher = StaticServiceProvider.Instance.GetRequiredService<IAvaloniaDispatcher>();

        if (dispatcher.CurrentApp?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.Windows.Any())
        {
            await messageBox.ShowDialog(desktop.Windows[0]);
        }
        else
        {
            var isClosed = false;
            messageBox.Closed += (sender, args) =>
            {
                isClosed = true;
            };
            messageBox.Show();
            await WaitHelper.WaitForAsync(() => isClosed, TimeSpan.MaxValue);
        }
        return viewModel.Result;

    }
    public MessageBox()
    {
        // This doesn't appear to work when set in XAML.
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}