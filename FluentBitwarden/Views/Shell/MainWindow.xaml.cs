using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Shell.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Views.Shell;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeService
{
    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();

        Closed += OnClosed;

        navigationService.Initialize(ContentFrame);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        Set(SettingsStore.Instance.Get(AppSettingKeys.Appearance.ThemeKey));
        ContentFrame.Navigate(typeof(LoadingPage));
    }

    public void Set(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
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
