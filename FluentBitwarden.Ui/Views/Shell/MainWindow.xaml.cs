using FluentBitwarden.Infrastructure.Window;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeChangeable
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void ApplyTheme(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

}
