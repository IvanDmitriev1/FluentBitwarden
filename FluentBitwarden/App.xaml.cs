using BitwaredApi;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Extensions;
using FluentBitwarden.Ui;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.ViewModels;
using FluentBitwarden.ViewModels.SetUp;
using FluentBitwarden.Views;
using FluentBitwarden.Views.SetUp;
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
                services.AddBitwaredCoreServices(BitwardenEnvironment.UnitedStates);
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

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow ??= Host.Services.GetRequiredService<MainWindow>();
        _mainWindow.Activate();

        var navigationService = Host.Services.GetRequiredService<INavigationService>();
        var authService = Host.Services.GetRequiredService<IAuthService>();

        try
        {
            var session = await authService.GetStoredSessionAsync();

            if (session is not null)
            {
                navigationService.Navigate<LoginPage>(clearBackStack: true);
            }
            else
            {
                navigationService.Navigate<SetupPage>(clearBackStack: true);
            }
        }
        catch
        {
            navigationService.Navigate<SetupPage>(clearBackStack: true);
        }
    }
}
