using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Shared.Services.Abstractions.Dialog;
using FluentBitwarden.Shared.Services.Implementations;
using FluentBitwarden.Views.Loading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;
using WinUIEx;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeService
{
    public MainWindow(NavigationService navigationService)
    {
        Instance = this;
        InitializeComponent();

        Closed += OnClosed;

        navigationService.Initialize(ContentFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        Apply(SettingsStore.Instance.Get(AppSettingKeys.Appearance.ThemeKey));
        ContentFrame.Navigate(typeof(LoadingPage));

    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    [field: MaybeNull]
    public static MainWindow Instance
    {
        get => field ?? throw new InvalidOperationException("MainWindow instance is not initialized");
        private set;
    }

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
        ContentFrame.BackStack.Clear();
        ContentFrame.ForwardStack.Clear();
        ContentFrame.Content = null;
    }

    private void RestoreResources()
    {
        if (ContentFrame.Content is not null)
            return;

        ContentFrame.Navigate(typeof(LoadingPage));
    }
}
