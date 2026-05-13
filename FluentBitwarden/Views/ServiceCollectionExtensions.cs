using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.Offline;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.LogIn;
using FluentBitwarden.Views.Unlock;
using FluentBitwarden.Views.Vault;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Views;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<ShellPage>();

        return services.AddView<LoadingPage, LoadingPageViewModel>()
            .AddView<OfflinePage, OfflinePageViewModel>()
            .AddView<LogInFlowPage, LogInFlowPageViewModel>()
            .AddView<UnlockPage, UnlockPageViewModel>()
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
