using FluentBitwarden.Infrastructure.Extensions;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace FluentBitwarden.Views.Passkey;

public sealed partial class OverlayWindow : WindowEx
{
    public OverlayWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragArea);

        this.PreventMaximizeOnTitleBarDoubleClick();
        this.CenterOnScreen();
    }

    public void SetContent(Page content)
    {
        Frame.Content = content;
    }

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
