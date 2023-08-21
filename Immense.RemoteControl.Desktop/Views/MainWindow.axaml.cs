using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Immense.RemoteControl.Desktop.Services;
using Immense.RemoteControl.Desktop.Shared;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Immense.RemoteControl.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = StaticServiceProvider.Instance?.GetService<IMainWindowViewModel>();

        InitializeComponent();

        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        var dispatcher = StaticServiceProvider.Instance?.GetService<IAvaloniaDispatcher>();
        dispatcher?.Shutdown();
    }
}
