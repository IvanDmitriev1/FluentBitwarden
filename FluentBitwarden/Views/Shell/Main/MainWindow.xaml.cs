using FluentBitwarden.Resources.AttachedProperties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Views.Startup;
using FluentBitwarden.Views.Startup.Models;
using FluentBitwarden.UI.Controls.Lifecycle;

namespace FluentBitwarden.Views.Shell.Main;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    private readonly IAppHostLifetimeService _appHostLifetimeService;

    public MainWindow(
        NavigationService navigationService,
        IAppHostLifetimeService appHostLifetimeService)
    {
        _appHostLifetimeService = appHostLifetimeService;

        InitializeComponent();
        Closed += OnClosed;

        TitlebarProperties.SetTargetTitleBar(AppTitleBar);
        navigationService.Initialize(RootFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        ApplyTheme(SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey));
        RootFrame.Navigate(
            typeof(LoadingPage),
            PageNavigationParameter.From(LoadingPageParameter.MainShell));
    }

    public bool IsHidden => !AppWindow.IsVisible;
    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void ApplyTheme(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        if (SettingsStore.Instance.Get(AppSettingKeys.App.CloseToTrayKey))
            return;

        IsShownInSwitchers = false;
        this.Hide();

        await _appHostLifetimeService.ShutdownAppHostAsync();
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
