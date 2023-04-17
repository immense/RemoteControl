using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Immense.RemoteControl.Desktop.Shared;
using Immense.RemoteControl.Desktop.Shared.Services;
using Immense.RemoteControl.Desktop.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Immense.RemoteControl.Desktop.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = StaticServiceProvider.Instance?.GetService<IMainWindowViewModel>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.FindControl<Border>(nameof(TitleBanner)).PointerPressed += TitleBanner_PointerPressed;
            this.FindControl<ListBox>(nameof(ViewerListBox)).SelectionChanged += ViewerListBox_SelectionChanged;
            Opened += MainWindow_Opened;
        }

        private void ViewerListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel &&
                sender is ListBox viewerListBox)
            {
                viewModel.SelectedViewers = viewerListBox.SelectedItems.Cast<IViewer>().ToList();
            }
        }

        private async void MainWindow_Opened(object? sender, System.EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.Init();
            }
        }

        private void TitleBanner_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }
    }
}
