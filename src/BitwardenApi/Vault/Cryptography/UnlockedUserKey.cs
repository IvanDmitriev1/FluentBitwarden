namespace BitwardenApi.Vault.Cryptography;

public sealed class UnlockedUserKey(UserId userId, byte[] userKey) : SymmetricCryptoKey(userKey)
{
    public UserId UserId { get; } = userId;
}
