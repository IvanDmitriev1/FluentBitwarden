using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultCipherIpcHandler(
    IAppSessionAccessor appSessionAccessor,
    IVaultService vaultService,
    IVaultCipherAttachmentService cipherAttachmentService) : IVaultCipherClient, IIpcRequestsHandler
{
    public Task<Cipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        var unlocked = appSessionAccessor.RequireUnlockedState();

        return Task.FromResult(unlocked.Vault.GetCiphers(query));
    }

    public Task<Cipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        var unlocked = appSessionAccessor.RequireUnlockedState();

        return Task.FromResult(unlocked.Vault.GetCipher(request.CipherId));
    }

    public async Task<Cipher> SaveCipherAsync(SaveVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        return await vaultService.SaveCipherAsync(
            request.Cipher,
            cancellationToken);
    }

    public async Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await cipherAttachmentService.DownloadAsync(
            request.CipherId,
            request.AttachmentId,
            request.DestinationPath,
            cancellationToken);
    }
}
