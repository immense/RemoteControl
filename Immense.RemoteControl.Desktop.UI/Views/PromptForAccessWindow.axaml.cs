using Avalonia.Controls;

namespace Immense.RemoteControl.Desktop.UI.Views;

public partial class PromptForAccessWindow : Window
{
    public PromptForAccessWindow()
    {
        InitializeComponent();
        Opened += Window_Opened;
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        Topmost = false;
    }

    private void TitleBanner_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
