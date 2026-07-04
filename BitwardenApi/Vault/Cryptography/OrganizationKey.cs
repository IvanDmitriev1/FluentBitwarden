using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

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
        using var bufferOwner = SpanOwner<byte>.Allocate(encryptedOrganizationKey.MaxPlaintextByteCount);
        Span<byte> buffer = bufferOwner.Span;
        try
        {
            int bytesWritten = privateKey.Decrypt(encryptedOrganizationKey, buffer);
            return new OrganizationKey(organizationId, buffer[..bytesWritten].ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }
}
