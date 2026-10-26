using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal sealed class TestAccountKeySession(UnlockedUserKey userKey) : IAccountKeySession
{
    public UserId UserId => userKey.UserId;

    public UnlockedUserKey UserKey => userKey;

    public void UpdateOrganizations(
        IReadOnlyDictionary<OrganizationId, AsymmetricEncString> organizationKeys) => throw new NotSupportedException();

    public SymmetricCryptoKey GetDecryptionKey(OrganizationId organizationId) => throw new NotSupportedException();

    public AttachmentKey CreateAttachmentKey(
        OrganizationId organizationId,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey) => throw new NotSupportedException();

    public void Dispose() => userKey.Dispose();
}
