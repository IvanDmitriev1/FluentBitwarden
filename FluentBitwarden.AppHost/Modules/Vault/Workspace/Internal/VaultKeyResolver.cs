using FluentBitwarden.AppHost.Modules.Accounts.Persistence;
using System.Security.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

internal sealed class VaultKeyResolver(
    DecryptedUserKey decryptedUserKey,
    AccountKeyMaterial accountKeyMaterial,
    VaultOrganizationDto[] organizations)
    : IDisposable
{
    private readonly Dictionary<OrganizationId, DecryptedOrganizationKey> _organizationKeysById = [];
    private readonly RSA _privateKey = CreatePrivateKey(decryptedUserKey, accountKeyMaterial);

    public void Dispose()
    {
        foreach (var key in _organizationKeysById.Values)
        {
            key.Dispose();
        }

        _privateKey.Dispose();
    }

    public DecryptedVaultKey GetKey(OrganizationId organizationId)
    {
        if (organizationId.IsEmpty)
            return decryptedUserKey;

        var id = organizationId;
        var organization = organizations.FirstOrDefault(candidate => candidate.Id == id);
        if (organization is null || organization.Id != id)
            throw new InvalidOperationException($"Organization key metadata is missing for organization {id}.");

        if (_organizationKeysById.TryGetValue(id, out var cachedKey))
            return cachedKey;

        var encryptedOrganizationKey = organization.EncryptedOrganizationKey;
        if (encryptedOrganizationKey.IsEmpty)
            throw new InvalidOperationException($"Organization {id} does not include an encrypted organization key.");

        var organizationKey = DecryptedOrganizationKey.Create(id, encryptedOrganizationKey, _privateKey);
        _organizationKeysById.Add(id, organizationKey);
        return organizationKey;
    }

    private static RSA CreatePrivateKey(DecryptedUserKey decryptedUserKey, AccountKeyMaterial accountKeyMaterial)
    {
        var encryptedPrivateKey = accountKeyMaterial.EncryptedPrivateKey.Value;
        using var privateKeyBufferOwner = SpanOwner<byte>.Allocate(encryptedPrivateKey.MaxPlaintextByteCount);
        Span<byte> privateKeyBuffer = privateKeyBufferOwner.Span;

        try
        {
            int bytesWritten = encryptedPrivateKey.DecodeTo(decryptedUserKey.Key, privateKeyBuffer);
            return ImportPrivateKey(privateKeyBuffer[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBuffer);
        }

        static RSA ImportPrivateKey(ReadOnlySpan<byte> privateKeyBytes)
        {
            try
            {
                var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                return rsa;
            }
            catch (CryptographicException pkcs8Exception)
            {
                try
                {
                    var rsa = RSA.Create();
                    rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                    return rsa;
                }
                catch (CryptographicException pkcs1Exception)
                {
                    throw new CryptographicException(
                        "The decrypted Bitwarden private key could not be imported as PKCS#8 or PKCS#1 RSA key.",
                        pkcs1Exception.InnerException ?? pkcs8Exception);
                }
            }
        }
    }
}
