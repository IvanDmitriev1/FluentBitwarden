using FluentBitwarden.AppHost.AppSession.Contracts;
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

    internal static IUnlockedSessionLease CreateLease(AccountProfile account, params Cipher[] ciphers)
    {
        var vault = Substitute.For<IUnlockedVault>();
        vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
        var lease = Substitute.For<IUnlockedSessionLease>();
        lease.Account.Returns(account);
        lease.Vault.Returns(vault);
        return lease;
    }
}
