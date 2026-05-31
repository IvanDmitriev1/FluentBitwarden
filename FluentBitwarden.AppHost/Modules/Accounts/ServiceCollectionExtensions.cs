using BitwardenApi.Contracts;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess;
using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Methods;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddSingleton<IStoredAccountStore, StoredAccountService>();
        services.AddSingleton<IAccountLoginService, AccountLoginService>();

        services.AddSingleton<WindowsHelloAccountUnlockMethod>();
        services.AddSingleton<MasterPasswordAccountUnlockMethod>();

        services.AddSingleton<IAccountAuthTokenProvider, AccountAuthTokenProvider>();

        services.AddSingleton<AccountUnlockService>();
        services.AddSingleton<IAccountUnlockService>(static sp => sp.GetRequiredService<AccountUnlockService>());
        services.AddSingleton<IUnlockedAccountKeyAccess>(static sp => sp.GetRequiredService<AccountUnlockService>());
        services.AddSingleton<IUnlockedAccountAccessor>(static sp => sp.GetRequiredService<AccountUnlockService>());
        services.AddSingleton<IBitwardenEnvironmentAccessor>(static sp => sp.GetRequiredService<AccountUnlockService>());

        services.AddIpcRequestHandler<AccountsClientHandler>();
        services.AddIpcRequestHandler<WindowsHelloUnlockClientHandler>();

        return services;
    }
}
