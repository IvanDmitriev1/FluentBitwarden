using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Accounts.LogIn;
using FluentBitwarden.Views.Accounts.Unlock;
using FluentBitwarden.Views.Vault;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Views.Shell.Main;
using FluentBitwarden.Views.Shell.Offline;
using FluentBitwarden.Views.Startup;

namespace FluentBitwarden.Views;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services)
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
