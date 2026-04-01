using FluentBitwarden.Modules.Security.Services.Unlock;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Session;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddTransient<BearerTokenHandler>();

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ITokenRefreshService, TokenRefreshService>();
        services.AddSingleton<ISessionTokensStore, ProtectedSessionTokensStore>();

        services.AddSingleton<CurrentSessionAccessor>();
        services.AddSingleton<ICurrentSessionAccessor>(static sp => sp.GetRequiredService<CurrentSessionAccessor>());

        return services;
    }
}
