using FluentBitwarden.Modules.AppState.Services;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Shell.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

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

    public void ReleaseWindowResources()
    {
        ContentFrame.BackStack.Clear();
        ContentFrame.ForwardStack.Clear();
        ContentFrame.Content = null;
    }

    public void RestoreResources()
    {
        if (ContentFrame.Content is not null)
            return;

        ContentFrame.Navigate(typeof(LoadingPage));
    }
}
