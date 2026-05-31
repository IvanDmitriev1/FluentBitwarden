using FluentBitwarden.Resources.AttachedProperties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Views.Shell.Loading;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    private readonly IAppHostLifetimeService _appHostLifetimeService;
    private readonly IUiHostedServiceStarter _hostedServiceStarter;

    public MainWindow(
        NavigationService navigationService,
        IAppHostLifetimeService appHostLifetimeService,
        IUiHostedServiceStarter hostedServiceStarter)
    {
        _appHostLifetimeService = appHostLifetimeService;
        _hostedServiceStarter = hostedServiceStarter;
        InitializeComponent();
        Closed += OnClosed;
        RootElement.Loaded += OnLoaded;

        TitlebarProperties.SetTargetTitleBar(AppTitleBar);
        navigationService.Initialize(RootFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        ApplyTheme(SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey));
        RootFrame.Navigate(typeof(LoadingPage));
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

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        RootElement.Loaded -= OnLoaded;
        await _hostedServiceStarter.EnsureStartedAsync();
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

        RootFrame.Navigate(typeof(LoadingPage));
    }

    private void RootFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        
    }
}
