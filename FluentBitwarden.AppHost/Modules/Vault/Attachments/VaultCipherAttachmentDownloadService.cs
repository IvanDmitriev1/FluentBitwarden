using BitwardenApi.Vault.Attachments;
using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.KeyResolution;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal sealed class VaultCipherAttachmentDownloadService(
    IVaultCipherAttachmentApi attachmentApi,
    IVaultSessionCoordinator vaultSessionCoordinator,
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultKeyResolverFactory keyResolverFactory) : IVaultCipherAttachmentDownloadService
{
    public async Task DownloadAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Attachment.CipherId.ThrowIfEmpty();
        request.Attachment.Id.ThrowIfEmpty();

        var unlockedSession = vaultSessionCoordinator.GetUnlockedSession();
        var userKey = unlockedSession.UserKey;
        userKey.UserId.ThrowIfEmpty();

        VaultCipherKeyMaterial cipherKeyMaterial;
        VaultKeyResolver keyResolver;

        // One unit of work reads the cipher key material and the resolver's key material in a single
        // transaction; the resolver holds no database handle after construction, so the unit of work
        // can be disposed before the download.
        using (var unitOfWork = unitOfWorkFactory.Create())
        {
            cipherKeyMaterial = unitOfWork.VaultReaderRepository.GetCipherKeyMaterial(userKey.UserId, request.Attachment.CipherId) ??
                                throw new InvalidOperationException($"Cipher key material is missing for cipher '{request.Attachment.CipherId}'.");

            keyResolver = keyResolverFactory.Create(unitOfWork, userKey);
        }

        using (keyResolver)
        {
            await attachmentApi.DownloadToAsync(
                unlockedSession.Account.BitwardenAccountContext,
                request.Attachment, (encStream, protectedAttachmentKey) =>
                {
                    using (keyResolver.CreateAttachmentKey(cipherKeyMaterial, protectedAttachmentKey))
                    {
                        return Task.CompletedTask;
                    }
                },
                cancellationToken);
        }
    }
}