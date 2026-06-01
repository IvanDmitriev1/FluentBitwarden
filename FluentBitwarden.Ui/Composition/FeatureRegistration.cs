using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Accounts.Unlock;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Views.Accounts.SignIn;
using FluentBitwarden.Views.Shell.Main;
using FluentBitwarden.Views.Shell.Offline;
using FluentBitwarden.Views.Startup.Loading;
using FluentBitwarden.Views.Vault.Browse;

namespace FluentBitwarden.Composition;

internal static class FeatureRegistration
{
    public static IServiceCollection AddFeatureViews(this IServiceCollection services)
    {
        services.AddTransient<ShellPage>();

        return services
            .AddView<LoadingPage, LoadingPageViewModel>()
            .AddView<UnlockPage, UnlockPageViewModel>()
            .AddView<OfflinePage, OfflinePageViewModel>()
            .AddView<LogInFlowPage, LogInFlowPageViewModel>()
            .AddView<SettingsPage, SettingsPageViewModel>()
            .AddView<VaultPage, VaultPageViewModel>();
    }

    private static IServiceCollection AddView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(this IServiceCollection services)
        where TPage : Page
        where TViewModel : ObservableObject
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();

        return services;
    }
}
