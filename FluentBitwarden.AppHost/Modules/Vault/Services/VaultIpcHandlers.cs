using BitwardenApi.Models;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal static class VaultIpcHandlers
{
    public static IServiceCollection MapVaultIpcHandlers(this IServiceCollection services)
    {
        services.AddIpcRequestHandler<VaultSyncResult>(IpcMessageTypes.Vault.Sync,
            static (IVaultService vaultService, CancellationToken ct) => vaultService.SyncVaultAsync(ct));

        services.AddIpcRequestHandler<VaultCipherQuery, VaultCipher[]>(static (VaultCipherQuery query,
            IVaultService vaultService) => vaultService.GetCiphers(query));

        services.AddIpcRequestHandler<GetVaultCipherRequest, IpcOptional<VaultCipher>>(static (
            GetVaultCipherRequest request, IVaultService vaultService) =>
        {
            var cipher = vaultService.GetCipher(request.CipherId);
            return new IpcOptional<VaultCipher>(cipher);
        });

        services.AddIpcRequestHandler<VaultFolder[]>(IpcMessageTypes.Vault.GetFolders,
            static (IVaultService vaultService) => vaultService.GetFolders());

        services.AddIpcRequestHandler<VaultCollection[]>(IpcMessageTypes.Vault.GetCollections,
            static (IVaultService vaultService) => vaultService.GetCollections());

        return services;
    }
}