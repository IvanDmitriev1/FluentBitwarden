using FluentBitwarden.Modules.AppState.Services;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Shell.Navigation;
using Microsoft.UI.Windowing;

namespace FluentBitwarden.Views.Shell;

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
