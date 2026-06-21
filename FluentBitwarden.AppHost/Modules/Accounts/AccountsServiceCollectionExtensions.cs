using FluentBitwarden.AppHost.Modules.Accounts.Authentication;
using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.AppHost.Modules.Accounts.Persistence;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal static class AccountsServiceCollectionExtensions
{
    public static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        services.AddSingleton<IAccountStore, AccountStore>();
        services.AddSingleton<IAccountLoginService, AccountLoginService>();

        services.AddSingleton<WindowsHelloKeyStore>();
        services.AddSingleton<WindowsHelloUnlocker>();
        services.AddSingleton<MasterPasswordUnlocker>();

        services.AddSingleton<IBitwardenAccessTokenProvider, AccountTokenProvider>();
        services.AddSingleton<IAccountUnlockService, AccountUnlockService>();

        return services;
    }
}
