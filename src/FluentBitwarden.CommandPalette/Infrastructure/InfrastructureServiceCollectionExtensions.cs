using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
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
        services.AddSingleton<IAccountClient, RemoteAccountClient>();
        services.AddSingleton<IAppSessionClient, RemoteAppSessionClient>();
        services.AddSingleton<IAccountWindowsHelloIntegrationClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultOperationsClient, RemoteVaultOperationsClient>();
        services.AddSingleton<IVaultCipherClient, RemoteVaultCipherClient>();
        services.AddSingleton<IVaultFolderClient, RemoteVaultFolderClient>();

        services.AddSingleton<IUiProcessManager, CommandPaletteUiProcessManager>();
        services.AddSingleton<IAppHostProcessManager, CommandPaletteAppHostProcessManager>();

        return services;
    }
}
