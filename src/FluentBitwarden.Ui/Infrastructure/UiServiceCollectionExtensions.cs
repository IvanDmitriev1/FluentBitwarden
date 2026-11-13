using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Features.Settings;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.Password;
using FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Shell.Controls;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Windows;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Navis.WinUI.Extensions;

namespace FluentBitwarden.Ui.Infrastructure;

internal static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddNavisNavigation();
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IAppRouter, AppRouter>();

        services.AddSingleton<AppSessionMonitor>();
        services.AddSingleton<IAppSession>(static sp => sp.GetRequiredService<AppSessionMonitor>());

        services.AddTransient<ISetupNavigator, SetupNavigator>();
        services.AddTransient<LoadingPageViewModel>();
        services.AddTransient<SetupEmailPageViewModel>();
        services.AddTransient<SetupPasswordPageViewModel>();
        services.AddTransient<SetupTwoFactorPageViewModel>();
        services.AddTransient<UnlockPageViewModel>();
        services.AddTransient<ShellPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<VaultPageViewModel>();
        services.AddTransient<VaultCipherSearchBoxViewModel>();

        return services;
    }
}
