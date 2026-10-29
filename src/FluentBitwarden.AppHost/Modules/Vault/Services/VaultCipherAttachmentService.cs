using BitwardenApi.Vault.Attachments;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultCipherAttachmentService(
    IAppSessionAccessor appSessionAccessor,
    IAttachmentsApi attachmentApi,
    VaultReaderRepository vaultReaderRepository) : IVaultCipherAttachmentService
{
    public Task DownloadAsync(
        CipherId cipherId,
        AttachmentId attachmentId,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var unlocked = appSessionAccessor.RequireUnlockedState();

        return attachmentApi.DownloadToAsync(
            unlocked.Account.BitwardenAccountContext,
            cipherId,
            attachmentId,
            async (stream, protectedAttachmentsKey) =>
            {
                var cipherKeyMaterial = vaultReaderRepository.GetCipherKeyMaterial(unlocked.Account.UserId, cipherId);
                using var decryptionKey = unlocked.AccountKeySession.CreateAttachmentKey(
                    cipherKeyMaterial.OrganizationId,
                    cipherKeyMaterial.ProtectedCipherKey,
                    protectedAttachmentsKey);

                await using var destinationFileStream = File.Create(destinationPath, 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await AttachmentDecryptor.DecryptToAsync(decryptionKey, stream, destinationFileStream,
                    cancellationToken);
            }, cancellationToken
        );
    }
}
