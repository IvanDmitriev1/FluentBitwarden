using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
namespace FluentBitwarden.Contracts.Modules.Vault.Ciphers;

public interface IVaultCipherClient
{
    Task<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default);

    Task<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default);

    Task<VaultCipher> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default);

    Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
