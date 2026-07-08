using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Accounts.KeyManagement;

/// <summary>
/// Session-scoped authority for the symmetric keys that decrypt vault data. Holds the unlocked
/// session's user key, derives and caches the private key once, and lazily decrypts and caches
/// organization keys for the session lifetime. Seeded on unlock via <see cref="BeginSession"/> and
/// torn down on lock via <see cref="EndSession"/>, mirroring the unlocked session's lifecycle.
/// </summary>
internal interface IAccountKeyService
{
    /// <summary>
    /// Opens a key session. Borrows <paramref name="userKey"/> (owned and disposed by the unlocked
    /// session, never disposed here); the derived private key and cached organization keys are owned
    /// and disposed by this service. Replaces any prior session, disposing it first.
    /// </summary>
    void BeginSession(UserKey userKey, ProtectedPrivateKey protectedPrivateKey);

    /// <summary>Closes the current key session, disposing the derived private key and cached
    /// organization keys. Idempotent.</summary>
    void EndSession();

    /// <summary>The unlocked session's user (personal) key.</summary>
    SymmetricCryptoKey UserKey { get; }

    /// <summary>
    /// The symmetric key for an organization. Returns the user key for an empty organization id;
    /// otherwise unwraps <paramref name="protectedOrganizationKey"/> with the session private key on
    /// first request and caches the decrypted key by id for the session lifetime (the protected key
    /// is ignored on a cache hit).
    /// </summary>
    SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey);

    /// <summary>
    /// Derives the attachment key for a cipher: resolves the cipher's base key (organization or user)
    /// from <paramref name="organizationId"/> and <paramref name="protectedOrganizationKey"/>, unwraps
    /// the cipher key, then unwraps the attachment key.
    /// </summary>
    AttachmentKey CreateAttachmentKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey);
}
