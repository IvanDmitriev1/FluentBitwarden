namespace BitwardenApi.Vault.Cryptography;

public sealed class OrganizationKey : SymmetricCryptoKey
{
    public OrganizationId OrganizationId { get; }

    private OrganizationKey(OrganizationId organizationId, byte[] key) : base(key)
        => OrganizationId = organizationId;

    public static OrganizationKey Create(
        OrganizationId organizationId,
        in AsymmetricEncString encryptedOrganizationKey,
        PrivateKey privateKey)
    {
        var key = privateKey.Decrypt(in encryptedOrganizationKey);
        return new OrganizationKey(organizationId, key);
    }
}
