using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.AppHost.Modules.Sessions.Models;

internal sealed class KeySession(UserKey userKey, ProtectedPrivateKey protectedPrivateKey) : IDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<OrganizationId, OrganizationKey> _organizationKeysById = [];
    private PrivateKey? _privateKey;

    private PrivateKey PrivateKey => _privateKey ??= CreatePrivateKey(userKey, protectedPrivateKey);

    public SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey)
    {
        if (organizationId.IsEmpty)
            return userKey;

        lock (_lock)
        {
            if (_organizationKeysById.TryGetValue(organizationId, out var cachedKey))
                return cachedKey;

            if (protectedOrganizationKey.IsEmpty)
            {
                throw new InvalidOperationException(
                    $"Organization '{organizationId}' does not include an encrypted organization key.");
            }

            var organizationKey = OrganizationKey.Create(
                organizationId,
                protectedOrganizationKey,
                PrivateKey);

            _organizationKeysById.Add(organizationId, organizationKey);
            return organizationKey;
        }
    }

    public AttachmentKey CreateAttachmentKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey)
    {
        var baseKey = GetOrganizationKey(organizationId, protectedOrganizationKey);
        using var cipherKey = CipherKey.Create(protectedCipherKey, baseKey);
        return AttachmentKey.Create(protectedAttachmentKey, cipherKey);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var key in _organizationKeysById.Values)
            {
                key.Dispose();
            }

            _organizationKeysById.Clear();
            _privateKey?.Dispose();
            _privateKey = null;
        }
    }

    private static PrivateKey CreatePrivateKey(UserKey userKey, ProtectedPrivateKey protectedPrivateKey)
    {
        var protectedPrivateKeyValue = protectedPrivateKey.Value;
        using var privateKeyBufferOwner = SpanOwner<byte>.Allocate(protectedPrivateKeyValue.MaxPlaintextByteCount);
        Span<byte> privateKeyBuffer = privateKeyBufferOwner.Span;

        try
        {
            int bytesWritten = protectedPrivateKeyValue.DecodeTo(userKey.Key, privateKeyBuffer);
            return PrivateKey.Import(privateKeyBuffer[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBuffer);
        }
    }
}
