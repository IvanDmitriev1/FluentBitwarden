using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Resources.AttachedProperties;
using FluentBitwarden.Views.Loading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Infrastructure.Services.Implementations;
using WinUIEx;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeService
{
    [field: MaybeNull]
    public static MainWindow Instance
    {
        get => field ?? throw new InvalidOperationException("MainWindow instance is not initialized");
        private set;
    }

    public MainWindow(NavigationService navigationService)
    {
        Instance = this;
        InitializeComponent();

        TitlebarProperties.SetTargetTitleBar(AppTitleBar);
        navigationService.Initialize(RootFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        Apply(SettingsStore.Instance.Get(AppSettingKeys.Appearance.ThemeKey));
        RootFrame.Navigate(typeof(LoadingPage));

        Closed += OnClosed;
    }

    public bool IsHidden => !AppWindow.IsVisible;
    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void Apply(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

    public void RequestExit()
    {
        Closed -= OnClosed;

        App.Current.Exit();
    }

    public void ShowWindow()
    {
        Activate();
        this.Show();
        BringToFront();
        IsShownInSwitchers = true;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        args.Handled = true;

        IsShownInSwitchers = false;
        this.Hide();
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
