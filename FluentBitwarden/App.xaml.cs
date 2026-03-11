using BitwaredApi;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Extensions;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Services;
using FluentBitwarden.Ui;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.ViewModels;
using FluentBitwarden.ViewModels.Setup;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using WinUI.DependencyInjection;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : Application, IXamlMetadataServiceProvider
{
    private MainWindow? _mainWindow;

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
                services.AddView<VaultPage, VaultPageViewModel>();
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
        if (_mainWindow is not null)
        {
            _mainWindow.Activate();
            return;
        }

        var vaultService = Host.Services.GetRequiredService<IVaultService>();
        var navigationService = Host.Services.GetRequiredService<INavigationService>();

        _mainWindow = Host.Services.GetRequiredService<MainWindow>();
        var splashScreen = new StartupSplashScreen(
            _mainWindow,
            Host.Services.GetRequiredService<ILocalDeviceInfoProvider>());

        splashScreen.Completed += async (sender, window) =>
        {
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
                    navigationService.Navigate<VaultPage>(clearBackStack: true);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported vault session state.");
            }
        };
    }
}
