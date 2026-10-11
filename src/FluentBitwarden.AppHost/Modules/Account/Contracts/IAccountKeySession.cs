using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountKeySession : IDisposable
{
    UserId UserId { get; }
    UnlockedUserKey UserKey { get; }

    void UpdateOrganizations(IReadOnlyDictionary<OrganizationId, AsymmetricEncString> organizationKeys);

    SymmetricCryptoKey GetDecryptionKey(OrganizationId organizationId);
    AttachmentKey CreateAttachmentKey(
        OrganizationId organizationId,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey);
}
