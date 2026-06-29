using FluentBitwarden.Infrastructure.Window;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace FluentBitwarden.Views.Shell;

public sealed partial class OverlayWindow : WinUIEx.WindowEx, IThemeChangeable
{
    public OverlayWindow()
    {
        InitializeComponent();

        this.PreventMaximizeOnTitleBarDoubleClick();
        this.CenterOnScreen();

        IsAlwaysOnTop = true;
        IsShownInSwitchers = false;
        IsResizable = false;
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;

        IsTitleBarVisible = false;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragArea);

    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;
    public Frame NavigationFrame => Frame;

    public void ApplyTheme(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
