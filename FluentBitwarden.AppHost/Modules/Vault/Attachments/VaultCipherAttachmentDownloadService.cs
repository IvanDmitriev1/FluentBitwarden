using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Attachments.Contracts;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.AppHost.Modules.Vault.KeyResolution;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal sealed class VaultCipherAttachmentDownloadService(
    IVaultCipherAttachmentApi attachmentApi,
    IVaultSessionCoordinator vaultSessionCoordinator,
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultKeyResolverFactory keyResolverFactory) : IVaultCipherAttachmentDownloadService
{
    public Task DownloadAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var unlockedSession = vaultSessionCoordinator.GetUnlockedSession();

        return attachmentApi.DownloadToAsync(
            unlockedSession.Account.BitwardenAccountContext,
            request.Attachment,
            async (encStream, protectedAttachmentKey) =>
            {
                using var attachmentKey = ResolveAttachmentKey(unlockedSession, request.Attachment, protectedAttachmentKey);

                await using var plaintextStream = new FileStream(
                    request.DestinationPath,
                    new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.ReadWrite,
                        Share = FileShare.None,
                        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                    });

                await EncFile.DecryptToAsync(attachmentKey, encStream, plaintextStream, cancellationToken);
            },
            cancellationToken);
    }


    private AttachmentKey ResolveAttachmentKey(UnlockedSession unlockedSession, VaultCipherAttachment attachment, EncString protectedAttachmentKey)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var cipherKeyMaterial = unitOfWork.VaultReaderRepository.GetCipherKeyMaterial(unlockedSession.UserKey.UserId, attachment.CipherId) ??
                                throw new InvalidOperationException($"Cipher key material is missing for cipher '{attachment.CipherId}'.");

        using var keyResolver = keyResolverFactory.Create(unitOfWork, unlockedSession.UserKey);

        var attachmentKey = keyResolver.CreateAttachmentKey(cipherKeyMaterial, protectedAttachmentKey);
        return attachmentKey;
    }
}