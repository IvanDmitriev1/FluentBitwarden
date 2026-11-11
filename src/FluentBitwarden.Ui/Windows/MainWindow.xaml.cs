using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.UI.Windowing;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeChangeable
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public Frame RootFrame => RootNavigationFrame;

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;
}
