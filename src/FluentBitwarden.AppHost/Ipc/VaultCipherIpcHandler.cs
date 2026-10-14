using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultCipherIpcHandler(
    IAppSessionService appSessionService,
    IVaultService vaultService,
    IVaultCipherAttachmentService cipherAttachmentService) : IVaultCipherClient, IIpcRequestsHandler
{
    public Task<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.RequireUnlockedSessionLease();

        return Task.FromResult(lease.Vault.GetCiphers(query));
    }

    public Task<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.RequireUnlockedSessionLease();

        return Task.FromResult(lease.Vault.GetCipher(request.CipherId));
    }

    public async Task<VaultCipher> SaveCipherAsync(SaveVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.RequireUnlockedSessionLease();

        return await vaultService.SaveCipherAsync(
            lease,
            request.Cipher,
            cancellationToken);
    }

    public async Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.RequireUnlockedSessionLease();

        await cipherAttachmentService.DownloadAsync(
            lease,
            request.CipherId,
            request.AttachmentId,
            request.DestinationPath,
            cancellationToken);
    }
}
