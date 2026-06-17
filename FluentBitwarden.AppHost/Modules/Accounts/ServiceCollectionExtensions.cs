using BitwardenApi.Identity;
using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Contracts;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Items;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess;
using FluentBitwarden.AppHost.Modules.Accounts.Login;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Methods;
using FluentBitwarden.Contracts.Infrastructure.Ipc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal static class ServiceCollectionExtensions
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "IPC registration intentionally reflects over known AppHost account handler methods.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "IPC registration intentionally closes known AppHost account handler invoker types at startup.")]
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddSingleton<IStoredAccountStore, StoredAccountService>();
        services.AddSingleton<IAccountLoginService, AccountLoginService>();

        services.AddSingleton<WindowsHelloAccountUnlockMethod>();
        services.AddSingleton<MasterPasswordAccountUnlockMethod>();

        services.AddSingleton<IAccountAuthTokenProvider, AccountAuthTokenProvider>();

        services.AddSingleton<AccountUnlockService>();
        services.AddSingleton<IAccountUnlockService>(static sp => sp.GetRequiredService<AccountUnlockService>());
        services.AddSingleton<IUnlockedAccountAccessor>(static sp => sp.GetRequiredService<AccountUnlockService>());
        services.AddSingleton<IBitwardenEnvironmentAccessor>(static sp => sp.GetRequiredService<AccountUnlockService>());

        services.AddIpcRequestHandler<AccountsClientHandler>();
        services.AddIpcRequestHandler<WindowsHelloUnlockClientHandler>();

        return services;
    }
}
