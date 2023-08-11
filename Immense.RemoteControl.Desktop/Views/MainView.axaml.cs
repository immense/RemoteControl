using Avalonia.Controls;
using Immense.RemoteControl.Desktop.Shared;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Immense.RemoteControl.Desktop.Views;
public partial class MainView : UserControl
{
    public MainView()
    {
        DataContext = StaticServiceProvider.Instance?.GetService<IMainViewViewModel>();

        InitializeComponent();
        this.FindControl<Border>(nameof(TitleBanner)).PointerPressed += TitleBanner_PointerPressed;
        this.FindControl<ListBox>(nameof(ViewerListBox)).SelectionChanged += ViewerListBox_SelectionChanged;

        Loaded += MainView_Loaded;
    }

    private void ViewerListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewViewModel viewModel &&
            sender is ListBox viewerListBox)
        {
            viewModel.SelectedViewers = viewerListBox.SelectedItems.Cast<IViewer>().ToList();
        }
    }

    private async void MainView_Loaded(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewViewModel viewModel)
        {
            await viewModel.Init();
        }
    }

    private void TitleBanner_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (Parent is not Window window)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
        {
            window.BeginMoveDrag(e);
        }
    }
}
