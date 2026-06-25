using FluentBitwarden.Views.Settings;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Views.Accounts;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Startup;
using FluentBitwarden.Views.Vault;

namespace FluentBitwarden.Views;

internal static class ViewRegistration
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<ShellPage>();
        services.AddTransient<LoadingPage>();
        services.AddTransient<OfflinePage>();

        return services
            .AddView<UnlockPage, UnlockPageViewModel>()
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
