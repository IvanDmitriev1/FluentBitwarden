using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.AppState.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.AppState;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppStateModule(this IServiceCollection services)
    {
        services.AddSingleton<IAppFirstRunService, AppFirstRunService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(static sp => sp.GetRequiredService<ThemeService>());

        return services;
    }
}