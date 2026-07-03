using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Vault.Cryptography;

public sealed class DecryptedOrganizationKey : DecryptedVaultKey
{
    public OrganizationId OrganizationId { get; }

    private DecryptedOrganizationKey(OrganizationId organizationId, byte[] key) : base(key)
        => OrganizationId = organizationId;

    public static DecryptedOrganizationKey Create(
        OrganizationId organizationId,
        in EncString encryptedOrganizationKey,
        RSA privateKey)
    {
        using var bufferOwner = SpanOwner<byte>.Allocate(encryptedOrganizationKey.MaxPlaintextByteCount);
        Span<byte> buffer = bufferOwner.Span;
        try
        {
            int bytesWritten = encryptedOrganizationKey.DecodeRsaTo(privateKey, buffer);
            return new DecryptedOrganizationKey(organizationId, buffer[..bytesWritten].ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }
}
