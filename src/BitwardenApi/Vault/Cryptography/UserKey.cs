namespace BitwardenApi.Vault.Cryptography;

public sealed class UserKey(UserId userId, byte[] userKey) : SymmetricCryptoKey(userKey)
{
    public UserId UserId { get; } = userId;
}
