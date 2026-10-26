using BitwardenApi.Client;
using BitwardenApi.Vault.Ciphers.Requests;
using BitwardenApi.Vault.Ciphers.Responses;
namespace BitwardenApi.Vault.Ciphers;

public interface ICiphersApi
{
    Task<CipherResponse> GetCipherAsync(BitwardenAccountContext accountContext, CipherId cipherId, CancellationToken cancellationToken = default);

    Task<CipherResponse> CreateCipherAsync(BitwardenAccountContext accountContext, CipherRequest request, CancellationToken cancellationToken = default);

    Task<CipherResponse> UpdateCipherAsync(BitwardenAccountContext accountContext, CipherId cipherId, CipherRequest request, CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(BitwardenAccountContext accountContext, CipherId cipherId, CancellationToken cancellationToken = default);
}
