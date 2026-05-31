using FluentBitwarden.Resources.AttachedProperties;
using FluentBitwarden.Views.Loading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics.CodeAnalysis;
using WinUIEx;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Contracts.Modules.AppState;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeService
{
    private readonly IAppHostLifetimeService _appHostLifetimeService;

    [field: MaybeNull]
    public static MainWindow Instance
    {
        get => field ?? throw new InvalidOperationException("MainWindow instance is not initialized");
        private set;
    }

    public MainWindow(
        NavigationService navigationService,
        IAppHostLifetimeService appHostLifetimeService)
    {
        _appHostLifetimeService = appHostLifetimeService;
        Instance = this;
        InitializeComponent();
        Closed += OnClosed;

        TitlebarProperties.SetTargetTitleBar(AppTitleBar);
        navigationService.Initialize(RootFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        Apply(SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey));
        RootFrame.Navigate(typeof(LoadingPage));
    }

    public bool IsHidden => !AppWindow.IsVisible;
    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void Apply(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

    public void ShowWindow()
    {
        IsShownInSwitchers = true;
        this.Show();
        this.Restore();

        bool focused = this.SetForegroundWindow();
        if (!focused)
            Activate();
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

        RootFrame.Navigate(typeof(LoadingPage));
    }

    private void RootFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        
    }
}
