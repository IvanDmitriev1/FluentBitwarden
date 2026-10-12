using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using Org.BouncyCastle.Crypto.Signers;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class Ed25519SshSignerTests
{
    [Fact]
    public void Sign_returns_a_signature_verifiable_from_the_public_key()
    {
        SyntheticOpenSshKey key = SshAgentTestData.CreateKey();
        byte[] data = "synthetic signing input"u8.ToArray();
        var request = new SshAgentSignRequest(key.PublicKeyBlob, data, SshAgentSignatureFlags.None);

        SshAgentSignature signature = Ed25519SshSigner.Sign(request, key.PrivateKeyPem);

        Assert.Equal("ssh-ed25519", signature.Algorithm);
        Assert.Equal(64, signature.Value.Length);
        var verifier = new Ed25519Signer();
        verifier.Init(false, key.PublicKey);
        verifier.BlockUpdate(data, 0, data.Length);
        Assert.True(verifier.VerifySignature(signature.Value));
    }

    [Fact]
    public void Sign_rejects_malformed_private_key_pem()
    {
        var request = new SshAgentSignRequest(new byte[] { 0x01 }, new byte[] { 0x02 }, SshAgentSignatureFlags.None);

        Assert.Throws<ArgumentException>(() => Ed25519SshSigner.Sign(request, "not a private key"));
    }

    [Fact]
    public void Sign_rejects_a_public_key_that_does_not_match_the_private_key()
    {
        SyntheticOpenSshKey privateKey = SshAgentTestData.CreateKey(0x31);
        SyntheticOpenSshKey otherKey = SshAgentTestData.CreateKey(0x71);
        var request = new SshAgentSignRequest(
            otherKey.PublicKeyBlob,
            new byte[] { 0x02 },
            SshAgentSignatureFlags.None);

        Assert.Throws<ArgumentException>(() => Ed25519SshSigner.Sign(request, privateKey.PrivateKeyPem));
    }

    [Theory]
    [InlineData((uint)SshAgentSignatureFlags.RsaSha2256)]
    [InlineData((uint)SshAgentSignatureFlags.RsaSha2512)]
    [InlineData(1u)]
    public void Sign_rejects_unsupported_signature_flags(uint flags)
    {
        SyntheticOpenSshKey key = SshAgentTestData.CreateKey();
        var request = new SshAgentSignRequest(
            key.PublicKeyBlob,
            new byte[] { 0x02 },
            (SshAgentSignatureFlags)flags);

        Assert.Throws<ArgumentException>(() => Ed25519SshSigner.Sign(request, key.PrivateKeyPem));
    }
}
