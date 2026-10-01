using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal sealed class TestAccountKeySession(UnlockedUserKey userKey) : IAccountKeySession
{
    public UserId UserId => userKey.UserId;

    public UnlockedUserKey UserKey => userKey;

    public SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        AsymmetricEncString protectedOrganizationKey) => throw new NotSupportedException();

    public AttachmentKey CreateAttachmentKey(
        AsymmetricEncString protectedOrganizationKey,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey) => throw new NotSupportedException();

    public void Dispose() => userKey.Dispose();
}
