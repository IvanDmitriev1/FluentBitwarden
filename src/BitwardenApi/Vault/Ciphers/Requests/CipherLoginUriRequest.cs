using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Ciphers.Requests;
public sealed class CipherLoginUriRequest
{
    public required EncString Uri { get; init; }

    /// <summary>URI match type: 0 = domain, 1 = host, ... null = default (domain).</summary>
    public int? Match { get; init; }
}
