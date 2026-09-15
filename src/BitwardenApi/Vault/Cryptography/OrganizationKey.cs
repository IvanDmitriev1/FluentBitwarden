using System.Security.Cryptography;

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
        byte[] key = privateKey.Decrypt(in encryptedOrganizationKey);
        try
        {
            return new OrganizationKey(organizationId, key);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(key);
            throw;
        }
    }
}