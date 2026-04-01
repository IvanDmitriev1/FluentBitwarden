using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Views.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Views;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services) =>
        services.AddView<LoadingPage, LoadingPageViewModel>()
            .AddView<SetupPage, SetupPageViewModel>()
            .AddView<UnlockPage, UnlockPageViewModel>()
            .AddView<SettingsPage, SettingsPageViewModel>()
            .AddView<VaultPage, VaultPageViewModel>();

    private static IServiceCollection AddView<TPage, TViewModel>(this IServiceCollection services)
        where TPage : Page
        where TViewModel : ObservableObject
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();

        return services;
    }
}
