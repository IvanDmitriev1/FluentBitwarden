using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Attachments.Contracts;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Sessions.Models;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

[Fody.ConfigureAwait(false)]
internal sealed class VaultCipherAttachmentDownloadService(
    IVaultCipherAttachmentApi attachmentApi,
    IVaultSessionManager sessionManager,
    IUnitOfWorkFactory unitOfWorkFactory) : IVaultCipherAttachmentDownloadService
{
    public Task DownloadAsync(
        BitwardenAccountContext accountContext,
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        return attachmentApi.DownloadToAsync(
            accountContext,
            request.Attachment,
            async (encStream, protectedAttachmentKey) =>
            {
                // The session's keys are borrowed under the gate for the derivation alone. The
                // decrypt below runs on the derived key, which this method owns, so the gate is not
                // held for the length of the transfer and a lock is never made to wait on it.
                // The attachment key cannot be derived any earlier: protectedAttachmentKey only
                // arrives with the download response.
                using var attachmentKey = await sessionManager.WithSessionAsync(
                    (session, _) => Task.FromResult<AttachmentKey?>(
                        ResolveAttachmentKey(
                            session.Account.UserId,
                            session.Keys,
                            request.Attachment,
                            protectedAttachmentKey)),
                    lockedResult: null,
                    cancellationToken)
                    ?? throw new InvalidOperationException(
                        "The vault was locked before the attachment could be decrypted.");

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
