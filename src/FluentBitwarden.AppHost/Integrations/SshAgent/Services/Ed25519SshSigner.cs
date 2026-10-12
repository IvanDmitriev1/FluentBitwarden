using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.OpenSsl;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class Ed25519SshSigner
{
    public static SshAgentSignature Sign(in SshAgentSignRequest request, string privateKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);

        using var strReader = new StringReader(privateKeyPem);
        var pemReader = new PemReader(strReader);

        var pem = pemReader.ReadPemObject();
        if (pem is null || pem.Type != "OPENSSH PRIVATE KEY")
        {
            throw new ArgumentException("The provided key is not a valid OpenSSH private key PEM.",
                nameof(privateKeyPem));
        }

        byte[] keyBlob = pem.Content;
        var privateKey = OpenSshPrivateKeyUtilities.ParsePrivateKeyBlob(keyBlob);

        if (privateKey is not Ed25519PrivateKeyParameters ed25519Key)
        {
            throw new ArgumentException(
                "The provided private key is not an Ed25519 key.",
                nameof(privateKeyPem));
        }

        var publicKeyBlob = OpenSshPublicKeyUtilities.EncodePublicKey(
            ed25519Key.GeneratePublicKey());

        if (!CanSign(request, publicKeyBlob))
        {
            throw new ArgumentException(
                "The signing request contains unsupported signature flags or the public key does not match the private key.",
                nameof(request));
        }

        byte[] signature = new byte[Ed25519PrivateKeyParameters.SignatureSize];

        ed25519Key.Sign(
            Ed25519.Algorithm.Ed25519,
            ctx: null,
            request.Data.Span,
            signature);

        return new SshAgentSignature("ssh-ed25519", signature);
    }

    private static bool CanSign(
        in SshAgentSignRequest request,
        ReadOnlySpan<byte> publicKeyBlob) =>
        request.Flags == SshAgentSignatureFlags.None &&
        request.PublicKeyBlob.Span.SequenceEqual(publicKeyBlob);
}
