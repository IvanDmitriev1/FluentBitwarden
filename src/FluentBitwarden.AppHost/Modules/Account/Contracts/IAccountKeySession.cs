using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountKeySession : IDisposable
{
    public UserId UserId { get; }

    public UnlockedUserKey UserKey { get; }

    public SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey);

    public AttachmentKey CreateAttachmentKey(
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey);
}
