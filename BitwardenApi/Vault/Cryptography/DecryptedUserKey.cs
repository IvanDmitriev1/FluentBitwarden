namespace BitwardenApi.Vault.Cryptography;

public sealed class DecryptedUserKey(UserId userId, byte[] userKey) : DecryptedVaultKey(userKey)
{
    public UserId UserId { get; } = userId;
}
