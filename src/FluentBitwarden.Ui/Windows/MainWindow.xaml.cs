using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using FluentBitwarden.Ui.Screens;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx, IDialogHost, IThemeChangeable
{
    public MainWindow()
    {
        ViewModel = App.Current.GetRequiredService<AppRootViewModel>();
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public AppRootViewModel ViewModel { get; }

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;
}
