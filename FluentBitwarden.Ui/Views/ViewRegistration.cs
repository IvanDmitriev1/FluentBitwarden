using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Views;

internal static class ViewRegistration
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<UnlockPageViewModel>();
        services.AddTransient<LogInFlowPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<VaultPageViewModel>();

        return services;
    }
}
