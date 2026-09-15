using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Attachments;

internal interface IVaultCipherAttachmentDownloadService
{
    /// <summary>
    /// Downloads an attachment and writes it decrypted to the request's destination path.
    /// </summary>
    /// <remarks>
    /// Resolves the session's key material itself, for the key derivation only — the caller must not
    /// hold the transition gate across this call. Locking the vault mid-download aborts it rather
    /// than landing a decrypted file on disk after the user asked to lock.
    /// </remarks>
    Task DownloadAsync(
        BitwardenAccountContext accountContext,
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
