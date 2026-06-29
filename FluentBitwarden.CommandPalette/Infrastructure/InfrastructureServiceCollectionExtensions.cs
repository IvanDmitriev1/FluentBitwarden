using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;
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
        services.AddSingleton<IWindowsHelloUnlockClient, RemoteWindowsHelloUnlockClient>();
        services.AddSingleton<IVaultClient, RemoteVaultClient>();

        services.AddSingleton<IUiProcessManager, CommandPaletteUiProcessManager>();
        services.AddSingleton<IAppHostProcessManager, CommandPaletteAppHostProcessManager>();

        return services;
    }
}
