using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Vault.Ciphers.Models;

public sealed class EncryptedPasswordHistory
{
    public required EncString Password { get; init; }
    public required DateTimeOffset LastUsedDate { get; init; }
}
