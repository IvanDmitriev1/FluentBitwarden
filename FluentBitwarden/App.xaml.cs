using BitwaredApi;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Extensions;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.ViewModels;
using FluentBitwarden.ViewModels.Login;
using FluentBitwarden.ViewModels.Setup;
using FluentBitwarden.ViewModels.Vault;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Login;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Vault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI.DependencyInjection;
using WinUIEx;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : Application, IXamlMetadataServiceProvider
{
    private static DispatcherQueue? _dispatcherQueue;
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private bool _isInitialized;

    public IHost Host { get; } =
        Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddBitwaredPlatformServices();
                services.AddBitwaredCoreServices();
                services.AddBitwaredWorkflowServices();
                services.AddUiServices();

                services.AddTransient<MainWindow>();
                services.AddView<LoginPage, LoginPageViewModel>();
                services.AddView<SettingsPage, SettingsPageViewModel>();
                services.AddView<SetupPage, SetupPageViewModel>();
                services.AddView<ShellPage, ShellPageViewModel>();

                services.AddView<VaultPage, VaultPageViewModel>();

                services.AddView<BlankPage1, BlankPage1ViewModel>();
                services.AddView<BlankPage2, BlankPage2ViewModel>();
            })
            .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        CreateSplashScreen();
        CreateTrayIcon();
    }

    public void QueueOnMainThread(Func<Task> task)
    {
        _dispatcherQueue?.TryEnqueue(async () =>
        {
            await task.Invoke();
        });
    }

    public Task ReopenWindowAsync()
    {
        var mainWindow = GetMainWindow();

        mainWindow.AppWindow.IsShownInSwitchers = true;
        mainWindow.Activate();
        mainWindow.BringToFront();

        return NavigateToPage();
    }

    private MainWindow GetMainWindow()
    {
        if (_mainWindow is not null)
            return _mainWindow;

        _mainWindow = Host.Services.GetRequiredService<MainWindow>();

        _mainWindow.AppWindow.Destroying += (sender, args) =>
        {
            _mainWindow = null;
        };

        var wm = WindowManager.Get(_mainWindow);
        wm.WindowStateChanged += (s, state) =>
        {
            wm.AppWindow.IsShownInSwitchers = state != WindowState.Minimized;
        };

        return _mainWindow;
    }

    private void CreateSplashScreen()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        MainWindow mainWindow = GetMainWindow();

        var splashScreen = new StartupSplashScreen(
            mainWindow,
            Host.Services);

        splashScreen.Completed += async (sender, window) =>
        {
            await NavigateToPage();
        };
    }

    private void CreateTrayIcon()
    {
        if (_trayIcon is not null)
            return;

        _trayIcon = new TrayIcon(1, "Assets/Bitwarden_icon.ico", "FluentBitwarden");
        _trayIcon.IsVisible = true;
        _trayIcon.Selected += async (s, e) =>
        {
            await ReopenWindowAsync();
        };

        _trayIcon.LeftDoubleClick += async (_, _) =>
        {
            await ReopenWindowAsync();
        };

        _trayIcon.ContextMenu += (_, e) =>
        {
            var flyout = new MenuFlyout();

            var showFlyoutItem = new MenuFlyoutItem() { Text = "Show" };
            showFlyoutItem.Click += async (_, _) =>
            {
                await ReopenWindowAsync();
            };
            flyout.Items.Add(showFlyoutItem);

            var lockFlyoutItem = new MenuFlyoutItem() { Text = "Lock" };
            showFlyoutItem.Click += (_, _) =>
            {
                
            };
            flyout.Items.Add(lockFlyoutItem);

            flyout.Items.Add(new MenuFlyoutSeparator());
            var exitFlyoutItem = new MenuFlyoutItem() { Text = "Exit"};
            exitFlyoutItem.Click += (_, _) => Exit();
            flyout.Items.Add(exitFlyoutItem);

            e.Flyout = flyout;
        };
    }

    private async Task NavigateToPage()
    {
        var vaultService = Host.Services.GetRequiredService<IVaultService>();
        var navigationService = Host.Services.GetRequiredService<INavigationService>();

        VaultSessionState state = await vaultService.GetSessionStateAsync().ConfigureAwait(true);

        switch (state)
        {
            case VaultSessionState.NoSession:
                navigationService.Navigate<SetupPage>(clearBackStack: true);
                break;

            case VaultSessionState.Locked:
                navigationService.Navigate<LoginPage>(clearBackStack: true);
                break;

            case VaultSessionState.Unlocked:
                navigationService.Navigate<ShellPage>(clearBackStack: true);
                break;

            default:
                throw new InvalidOperationException("Unsupported vault session state.");
        }
    }
}
