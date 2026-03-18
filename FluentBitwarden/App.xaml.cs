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
using System.Text;
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
    public new static App Current => (App)Application.Current;

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
        ValidationTrimDependencies.Preserve();

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

        var startupSplashScreen = new StartupSplashScreen(
            mainWindow,
            Host.Services);

        startupSplashScreen.Completed += async (sender, window) =>
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
            lockFlyoutItem.Click += async (_, _) =>
            {
                var vaultService = Host.Services.GetRequiredService<IVaultService>();
                var navigationService = Host.Services.GetRequiredService<INavigationService>();

                await vaultService.LockAsync();
                navigationService.Navigate<LoginPage>(clearBackStack: true);
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

    public static void WriteException(Exception e)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logFilePath = Path.Combine(localAppData, "FluentBitwarden", "Logs", "unhandled-exceptions.log");
        string? logDirectory = Path.GetDirectoryName(logFilePath);

        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        File.AppendAllText(logFilePath, BuildUnhandledExceptionLogEntry(e), Encoding.UTF8);
    }

    private static string BuildUnhandledExceptionLogEntry(Exception exception)
    {
        StringBuilder builder = new();
       

        builder.AppendLine(new string('-', 80));
        builder.Append("Timestamp: ").AppendLine(DateTimeOffset.Now.ToString("O"));
        builder.AppendLine("Source: Application.UnhandledException");
        builder.Append("Packaged: ").AppendLine(PackageHelper.IsPackaged.ToString());
        builder.Append("ProcessId: ").AppendLine(Environment.ProcessId.ToString());
        builder.Append("BaseDirectory: ").AppendLine(AppContext.BaseDirectory);

        builder.Append("ExceptionType: ").AppendLine(exception.GetType().FullName ?? exception.GetType().Name);
        builder.Append("ExceptionMessage: ").AppendLine(exception.Message);
        builder.AppendLine();
        builder.AppendLine(exception.ToString());

        builder.AppendLine();
        return builder.ToString();
    }
}
