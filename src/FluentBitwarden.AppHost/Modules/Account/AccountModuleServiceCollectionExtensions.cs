using FluentBitwarden.AppHost.Modules.Account.Contracts;
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

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAccountWindowsHelloService, AccountWindowsHelloService>();
    }
}
