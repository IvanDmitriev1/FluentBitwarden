using FluentBitwarden.AttachedProperties;
using FluentBitwarden.Infrastructure.Window;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeChangeable
{
    public MainWindow()
    {
        InitializeComponent();

        TitlebarProperties.SetTargetTitleBar(AppTitleBar);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        this.Maximize();
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;
    public Frame NavigationFrame => RootFrame;

    public void ApplyTheme(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

}
