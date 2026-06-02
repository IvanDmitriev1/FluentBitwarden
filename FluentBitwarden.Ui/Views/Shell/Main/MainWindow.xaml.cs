using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using FluentBitwarden.Views.Startup.Loading;
using FluentBitwarden.Platform;

namespace FluentBitwarden.Views.Shell.Main;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow(
        NavigationService navigationService)
    {
        InitializeComponent();

        TitlebarProperties.SetTargetTitleBar(AppTitleBar);
        navigationService.Initialize(RootFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        ApplyTheme(SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey));
        RootFrame.Navigate(
            typeof(LoadingPage),
            PageNavigationParameter.From(LoadingPageParameter.MainShell));
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void ApplyTheme(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

    private void ReleaseWindowResources()
    {
        RootFrame.BackStack.Clear();
        RootFrame.ForwardStack.Clear();
        RootFrame.Content = null;
    }

    private void RestoreResources()
    {
        if (RootFrame.Content is not null)
            return;

        RootFrame.Navigate(
            typeof(LoadingPage),
            PageNavigationParameter.From(LoadingPageParameter.MainShell));
    }

    private void RootFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        
    }
}
