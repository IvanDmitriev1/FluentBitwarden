using BitwardenApi.Cryptography.Encryption;

namespace BitwardenApi.Vault.Ciphers.Models;

public sealed class EncryptedLoginUri
{
    public required EncString Uri { get; init; }
    public string? UriChecksum { get; init; }
    public int? Match { get; init; }
}
