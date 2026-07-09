using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Attachments.Contracts;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal sealed class VaultCipherAttachmentDownloadService(
    IVaultCipherAttachmentApi attachmentApi,
    IUnitOfWorkFactory unitOfWorkFactory) : IVaultCipherAttachmentDownloadService
{
    public Task DownloadAsync(
        BitwardenAccountContext accountContext,
        KeySession keys,
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        return attachmentApi.DownloadToAsync(
            accountContext,
            request.Attachment,
            async (encStream, protectedAttachmentKey) =>
            {
                using var attachmentKey = ResolveAttachmentKey(accountContext.UserId, keys, request.Attachment, protectedAttachmentKey);

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


    private AttachmentKey ResolveAttachmentKey(
        UserId userId,
        KeySession keys,
        VaultCipherAttachment attachment,
        EncString protectedAttachmentKey)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var cipher = unitOfWork.VaultReaderRepository.GetCipherKeyMaterial(userId, attachment.CipherId) ??
                     throw new InvalidOperationException($"Cipher key material is missing for cipher '{attachment.CipherId}'.");

        cipher.CipherId.ThrowIfEmpty();

        var protectedOrganizationKey = cipher.OrganizationId.IsEmpty
            ? AsymmetricEncString.Empty
            : unitOfWork.VaultReaderRepository.GetAllOrganizations(userId)
                .First(organization => organization.Id == cipher.OrganizationId)
                .ProtectedOrganizationKey;

        return keys.CreateAttachmentKey(
            cipher.OrganizationId,
            protectedOrganizationKey,
            cipher.ProtectedCipherKey,
            protectedAttachmentKey);
    }
}
