using FluentBitwarden.Modules.AppState.Services;
using FluentBitwarden.Shell.Navigation;
using FluentBitwarden.Views.Loading;
using Microsoft.UI.Windowing;

namespace FluentBitwarden.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow(NavigationService navigationService, ThemeService themeService)
    {
        InitializeComponent();

        themeService.Initialize(RootElement);
        navigationService.Initialize(ContentFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        ContentFrame.Navigate(typeof(LoadingPage));
    }
}
