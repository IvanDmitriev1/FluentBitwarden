using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Session;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<AccountSessionManager>();
        services.AddSingleton<IAccountSignInService, AccountSignInService>();
        services.AddSingleton<IAccountSessionTokensStore, AccountSessionTokensStore>();

        services.AddSingleton<IAccountSessionManager>(static sp => sp.GetRequiredService<AccountSessionManager>());
        services.AddSingleton<IBitwardenEnvironmentAccessor>(static sp => sp.GetRequiredService<AccountSessionManager>());

        services.AddTransient<BearerAuthTokenProvider>();

        return services;
    }
}