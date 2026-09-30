using BitwardenApi.Identity.Contracts;
using BitwardenApi.Infrastructure.Cryptography;
using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.Modules.Account.Internal;

internal sealed class AccountKeySession(UnlockedUserKey userKey, ProtectedPrivateKey protectedPrivateKey) : IAccountKeySession
{
    private readonly Dictionary<OrganizationId, OrganizationKey> _organizationKeysById = [];
    private bool _disposed;

    private PrivateKey PrivateKey { get; } = userKey.CreatePrivateKey(protectedPrivateKey);
    
    public UnlockedUserKey UserKey { get; } = userKey;

    public UserId UserId => UserKey.UserId;

    public SymmetricCryptoKey GetOrganizationKey(OrganizationId organizationId, AsymmetricEncString protectedOrganizationKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (organizationId.IsEmpty)
            return UserKey;

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

    public AttachmentKey CreateAttachmentKey(
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var cipherKey = CipherKey.Create(protectedCipherKey, UserKey);
        return AttachmentKey.Create(protectedAttachmentKey, cipherKey);
    }

    public void Dispose()
    {
        if (!Interlocked.Exchange(ref _disposed, true))
            return;

        foreach (var key in _organizationKeysById.Values)
        {
            key.Dispose();
        }

        _organizationKeysById.Clear();
        PrivateKey.Dispose();
    }
}
