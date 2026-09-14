using Microsoft.Extensions.DependencyInjection;
using FluentBitwarden.ViewModels.Shell;
using FluentBitwarden.ViewModels.Startup;

namespace FluentBitwarden.Views;

internal static class ViewRegistration
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<UnlockPageViewModel>();
        services.AddTransient<LogInFlowPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<VaultPageViewModel>();
        services.AddTransient<ShellPageViewModel>();
        services.AddTransient<LoadingPageViewModel>();

        return services;
    }
}
