using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Vault.Ciphers.Models;

public sealed class EncryptedCipherField
{
    public required EncString Name { get; init; }
    public required EncString Value { get; init; }
    public int Type { get; init; }
    public int? LinkedId { get; init; }
}
