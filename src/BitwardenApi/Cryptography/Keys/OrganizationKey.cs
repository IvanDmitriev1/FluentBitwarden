using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Cryptography.Keys;

public sealed class OrganizationKey : SymmetricCryptoKey
{
    public OrganizationId OrganizationId { get; }

    private OrganizationKey(OrganizationId organizationId, byte[] key) : base(key)
        => OrganizationId = organizationId;

    public static OrganizationKey Create(
        OrganizationId organizationId,
        in AsymmetricEncString encryptedOrganizationKey,
        RsaPrivateKey privateKey)
    {
        var key = privateKey.Decrypt(in encryptedOrganizationKey);
        return new OrganizationKey(organizationId, key);
    }
}
