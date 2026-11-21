using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.UI.Windowing;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeChangeable, IContentDialogHost
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    public Frame RootFrame => RootNavigationFrame;

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;

    public Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken)
    {
        dialog.XamlRoot = RootElement.XamlRoot;
        return dialog.ShowAsync(ContentDialogPlacement.Popup).AsTask(cancellationToken);
    }
}
