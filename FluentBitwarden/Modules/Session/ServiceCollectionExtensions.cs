using BitwardenApi.Modules.Notifications.Abstractions;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using FluentBitwarden.Modules.Session.Services.Authentication;
using FluentBitwarden.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Session;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddTransient<BearerTokenHandler>();
        services.AddSingleton<ISignalRAccessTokenProvider, SignalRAccessTokenProvider>();

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ITokenRefreshService, TokenRefreshService>();

        services.AddSingleton<CurrentSessionAccessor>();
        services.AddSingleton<ICurrentSessionAccessor>(static sp => sp.GetRequiredService<CurrentSessionAccessor>());
        services.AddSingleton<IBitwardenEnvironmentAccessor>(static sp => sp.GetRequiredService<ICurrentSessionAccessor>());

        if (PackageHelper.IsPackaged)
            services.AddSingleton<ISessionTokensStore, PasswordVaultSessionTokensStore>();
        else
            services.AddSingleton<ISessionTokensStore, DpApiSessionTokensStore>();

        return services;
    }
}
