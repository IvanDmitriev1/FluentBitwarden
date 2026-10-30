using BitwardenApi.Client;
using BitwardenApi.Vault.Ciphers.Requests;
using BitwardenApi.Vault.Ciphers.Responses;

namespace BitwardenApi.Vault.Ciphers;

public interface ICiphersApi
{
    Task<CipherResponse> GetCipherAsync(BitwardenAccountContext accountContext, CipherId cipherId, CancellationToken cancellationToken = default);

    Task<CipherResponse> GetCipherDetailsAsync(BitwardenAccountContext accountContext, CipherId cipherId, CancellationToken cancellationToken = default);

    Task<CipherResponse> CreateCipherAsync(BitwardenAccountContext accountContext, CipherRequest request, CancellationToken cancellationToken = default);

    Task<CipherResponse> CreateCipherAsync(BitwardenAccountContext accountContext, CipherRequest request, IReadOnlyList<CollectionId> collectionIds, CancellationToken cancellationToken = default);

    Task<CipherResponse> UpdateCipherAsync(BitwardenAccountContext accountContext, CipherId cipherId, CipherRequest request, CancellationToken cancellationToken = default);

    Task<CipherResponse> UpdateCipherPartialAsync(BitwardenAccountContext accountContext, CipherId cipherId, FolderId folderId, bool favorite, CancellationToken cancellationToken = default);

    Task DeleteCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds, CancellationToken cancellationToken = default);

    Task SoftDeleteCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds, CancellationToken cancellationToken = default);

    Task RestoreCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds, CancellationToken cancellationToken = default);

    Task UnarchiveCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds, CancellationToken cancellationToken = default);

    Task ArchiveCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds, CancellationToken cancellationToken = default);

    Task MoveCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds, FolderId folderId, CancellationToken cancellationToken = default);

    Task ShareCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherRequest> ciphers, IReadOnlyList<CollectionId> collectionIds, CancellationToken cancellationToken = default);

}
