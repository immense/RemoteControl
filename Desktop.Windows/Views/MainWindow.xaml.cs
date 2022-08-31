using Immense.RemoteControl.Desktop.Windows.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Button = System.Windows.Controls.Button;
using ToolTip = System.Windows.Controls.ToolTip;

namespace Immense.RemoteControl.Desktop.Windows.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CopyLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            viewModel.CopyLink();
            var tooltip = new ToolTip
            {
                PlacementTarget = sender as Button,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 5,
                Content = "Copied to clipboard!",
                HasDropShadow = true,
                StaysOpen = false,
                IsOpen = true
            };

            await Task.Delay(750);
            var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(750));
            tooltip.BeginAnimation(OpacityProperty, animation);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button senderButton)
            {
                senderButton.ContextMenu.IsOpen = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShutdownApp();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this) &&
                DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.Init();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
