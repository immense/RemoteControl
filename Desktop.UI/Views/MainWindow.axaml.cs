using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Immense.RemoteControl.Desktop.UI.ViewModels;

namespace Immense.RemoteControl.Desktop.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Current = this;

            InitializeComponent();
        }

        public static MainWindow? Current { get; set; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.FindControl<Border>("TitleBanner").PointerPressed += TitleBanner_PointerPressed;

            Opened += MainWindow_Opened;
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
