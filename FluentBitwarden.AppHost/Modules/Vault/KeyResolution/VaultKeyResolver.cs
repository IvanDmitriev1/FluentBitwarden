using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.AppHost.Modules.Vault.KeyResolution;

/// <summary>
/// Resolves the symmetric key that decrypts a vault item: the user key for personal items, or a
/// lazily-decrypted, cached organization key for shared items.
/// Ownership: borrows the <see cref="UserKey"/> (owned by the unlocked session, never disposed here);
/// owns and disposes the cached organization keys and the imported private key.
/// The resolver reads no database after construction, so the unit of work used to build it may be
/// disposed immediately.
/// </summary>
internal sealed class VaultKeyResolver : IDisposable
{
    private readonly UserKey _userKey;
    private readonly ProtectedPrivateKey _protectedPrivateKey;
    private readonly Dictionary<OrganizationId, AsymmetricEncString> _protectedOrganizationKeysById;
    private readonly Dictionary<OrganizationId, OrganizationKey> _organizationKeysById = [];
    private bool _disposed;

    private PrivateKey? _privateKey;

    private PrivateKey PrivateKey
    {
        get
        {
            ThrowIfDisposed();
            return _privateKey ??= CreatePrivateKey(_userKey, _protectedPrivateKey);
        }
    }

    internal VaultKeyResolver(
        UserKey userKey,
        ProtectedPrivateKey protectedPrivateKey,
        VaultOrganizationDto[] organizations)
    {
        _userKey = userKey;
        _protectedPrivateKey = protectedPrivateKey;
        _protectedOrganizationKeysById = organizations.ToDictionary(
            static organization => organization.Id,
            static organization => organization.ProtectedOrganizationKey);
    }

    public SymmetricCryptoKey GetKeyForOrganization(OrganizationId organizationId)
    {
        ThrowIfDisposed();

        if (organizationId.IsEmpty)
            return _userKey;

        if (_organizationKeysById.TryGetValue(organizationId, out var cachedKey))
            return cachedKey;

        var protectedOrganizationKey = GetProtectedOrganizationKey(organizationId);
        var organizationKey = OrganizationKey.Create(
            organizationId,
            protectedOrganizationKey,
            PrivateKey);

        _organizationKeysById.Add(organizationId, organizationKey);
        return organizationKey;
    }

    public AttachmentKey CreateAttachmentKey(
        VaultCipherKeyMaterial cipher,
        EncString protectedAttachmentKey)
    {
        ThrowIfDisposed();
        cipher.CipherId.ThrowIfEmpty();

        var baseKey = GetKeyForOrganization(cipher.OrganizationId);
        using var cipherKey = CipherKey.Create(cipher.ProtectedCipherKey, baseKey);
        return AttachmentKey.Create(protectedAttachmentKey, cipherKey);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var key in _organizationKeysById.Values)
        {
            key.Dispose();
        }

        _privateKey?.Dispose();
    }

    private AsymmetricEncString GetProtectedOrganizationKey(OrganizationId organizationId)
    {
        organizationId.ThrowIfEmpty();

        if (!_protectedOrganizationKeysById.TryGetValue(organizationId, out var protectedOrganizationKey))
        {
            throw new InvalidOperationException(
                $"Organization key metadata is missing for organization '{organizationId}'.");
        }

        if (protectedOrganizationKey.IsEmpty)
        {
            throw new InvalidOperationException(
                $"Organization '{organizationId}' does not include an encrypted organization key.");
        }

        return protectedOrganizationKey;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

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
