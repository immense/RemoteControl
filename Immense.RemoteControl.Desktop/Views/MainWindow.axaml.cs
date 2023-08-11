using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Immense.RemoteControl.Desktop.Shared;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Immense.RemoteControl.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = StaticServiceProvider.Instance?.GetService<IMainWindowViewModel>();

        InitializeComponent();
    }
}
