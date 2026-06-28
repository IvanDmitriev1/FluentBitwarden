using FluentBitwarden.CommandPalette.Infrastructure.Services;
using FluentBitwarden.Contracts.Infrastructure;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Platform.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.CommandPalette.Infrastructure;

internal static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddIpcClient(IpcConstants.AppHostPipeName);
        services.AddIpcEventClient(IpcConstants.AppHostEventsPipeName);
        services.AddSingleton<IAccountsClient, RemoteAccountsClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddSingleton<IVaultSessionUnlockDialog, VaultSessionUnlockDialog>();


        services.AddSingleton<IUiProcessManager, ComPlateExtUiProcessManager>();
        services.AddSingleton<IAppHostProcessManager, ComPlateExtAppHostProcessManager>();

        return services;
    }
}
