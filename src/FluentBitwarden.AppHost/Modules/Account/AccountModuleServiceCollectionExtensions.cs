using BitwardenApi.Infrastructure.Transport;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;
using FluentBitwarden.AppHost.Modules.Account.Persistance;
using FluentBitwarden.AppHost.Modules.Account.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Account;

internal static class AccountModuleServiceCollectionExtensions
{
    public static void AddAccountModule(this IServiceCollection services)
    {
        services.AddScoped<AccountBitwardenSessionTokenRepository>();
        services.AddScoped<AccountKeyMaterialRepository>();
        services.AddScoped<AccountProfileRepository>();
        services.AddScoped<AccountTpmUnlockKeyRepository>();

        services.AddSingleton<AccountAuthenticatorService>();
        services.AddSingleton<AccountTokenCache>();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAccountWindowsHelloService, AccountWindowsHelloService>();
        services.AddScoped<IBitwardenAccessTokenProvider, AccountSessionAccessTokenProvider>();
    }
}
