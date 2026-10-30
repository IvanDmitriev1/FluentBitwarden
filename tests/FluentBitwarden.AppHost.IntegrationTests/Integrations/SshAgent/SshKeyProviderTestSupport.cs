using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using Org.BouncyCastle.Crypto.Signers;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

internal static class SshKeyProviderTestSupport
{
    internal static void AssertSignature(SyntheticOpenSshKey key, byte[] data, SshAgentSignature signature)
    {
        Assert.Equal("ssh-ed25519", signature.Algorithm);
        var verifier = new Ed25519Signer();
        verifier.Init(false, key.PublicKey);
        verifier.BlockUpdate(data, 0, data.Length);
        Assert.True(verifier.VerifySignature(signature.Value));
    }

    internal static IAppSessionAccessor CreateLease(AccountProfile account, params Cipher[] ciphers)
    {
        var vault = Substitute.For<IUnlockedVault>();
        vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
        var lease = Substitute.For<IAppSessionAccessor>();
        var key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(account.UserId);
        var state = new AppSessionState.Unlocked(account, vault, key, Substitute.For<IServerNotificationsDispatcher>());
        lease.AppSessionState.Returns(state);
        return lease;
    }
}
