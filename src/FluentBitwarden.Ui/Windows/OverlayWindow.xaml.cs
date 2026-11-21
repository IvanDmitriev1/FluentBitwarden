using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class OverlayWindow : WinUIEx.WindowEx, IThemeChangeable, IContentDialogHost
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

    public Frame RootFrame => RootNavigationFrame;

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;

    public Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken)
    {
        dialog.XamlRoot = RootElement.XamlRoot;
        Grid.SetRow(dialog, Grid.GetRow(RootNavigationFrame));
        RootElement.Children.Add(dialog);

        return dialog.ShowAsync(ContentDialogPlacement.InPlace).AsTask(cancellationToken);
    }

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
