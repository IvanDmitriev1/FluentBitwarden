using FluentBitwarden.Ui.Infrastructure.Windowing;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using FluentBitwarden.Ui.Screens;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class OverlayWindow : WinUIEx.WindowEx, IDialogHost, IThemeChangeable
{
    public OverlayWindow()
    {
        ViewModel = App.Current.GetRequiredService<AppRootViewModel>();
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

    public AppRootViewModel ViewModel { get; }
    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
