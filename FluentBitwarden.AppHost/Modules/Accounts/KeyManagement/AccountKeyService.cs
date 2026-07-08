using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Accounts.KeyManagement;

/// <inheritdoc cref="IAccountKeyService"/>
internal sealed class AccountKeyService : IAccountKeyService
{
    private KeySession? _session;

    public void BeginSession(UserKey userKey, ProtectedPrivateKey protectedPrivateKey)
    {
        var previous = Interlocked.Exchange(ref _session, new KeySession(userKey, protectedPrivateKey));
        previous?.Dispose();
    }

    public void EndSession()
    {
        var previous = Interlocked.Exchange(ref _session, null);
        previous?.Dispose();
    }

    public SymmetricCryptoKey UserKey => RequireSession().UserKey;

    public SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey) =>
        RequireSession().GetOrganizationKey(organizationId, protectedOrganizationKey);

    public AttachmentKey CreateAttachmentKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey) =>
        RequireSession().CreateAttachmentKey(
            organizationId,
            protectedOrganizationKey,
            protectedCipherKey,
            protectedAttachmentKey);

    private KeySession RequireSession() =>
        Volatile.Read(ref _session) ?? throw new InvalidOperationException("No unlocked account is present.");
}