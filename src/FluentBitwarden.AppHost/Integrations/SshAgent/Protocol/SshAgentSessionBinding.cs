using Org.BouncyCastle.Math.EC.Rfc8032;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal readonly record struct SshAgentSessionBinding(
    ReadOnlyMemory<byte> HostKeyBlob,
    ReadOnlyMemory<byte> SessionId)
{
    private const int MaxSessionIdLength = 128;

    public static bool IsForwardingBinding(ReadOnlyMemory<byte> data)
    {
        var reader = new SshAgentPayloadReader(data);
        return reader.TryReadString(out _) &&
            reader.TryReadString(out _) &&
            reader.TryReadString(out _) &&
            reader.TryReadByte(out byte isForwarding) && isForwarding == 1 &&
            reader.End;
    }

    public static bool TryParseAndVerify(ReadOnlyMemory<byte> data, out SshAgentSessionBinding binding)
    {
        binding = default;
        var reader = new SshAgentPayloadReader(data);

        if (!reader.TryReadString(out var hostKeyBlob) ||
            !reader.TryReadString(out var sessionId) || sessionId.IsEmpty || sessionId.Length > MaxSessionIdLength ||
            !reader.TryReadString(out var signatureBlob) ||
            !reader.TryReadByte(out byte isForwarding) || isForwarding != 0 ||
            !reader.End ||
            !TryReadPublicKey(hostKeyBlob, out var publicKeyBytes) ||
            !TryReadSignature(signatureBlob, out var signature))
        {
            return false;
        }

        if (!Ed25519.Verify(signature.Span, publicKeyBytes.Span, sessionId.Span))
            return false;

        binding = new SshAgentSessionBinding(hostKeyBlob, sessionId);
        return true;
    }

    private static bool TryReadPublicKey(
        ReadOnlyMemory<byte> hostKeyBlob,
        out ReadOnlyMemory<byte> keyBytes)
    {
        keyBytes = default;
        var reader = new SshAgentPayloadReader(hostKeyBlob);
        if (!reader.TryReadString(out var algorithm) || !algorithm.Span.SequenceEqual("ssh-ed25519"u8) ||
            !reader.TryReadString(out keyBytes) || keyBytes.Length != Ed25519.PublicKeySize ||
            !reader.End)
        {
            return false;
        }

        return true;
    }

    private static bool TryReadSignature(ReadOnlyMemory<byte> signatureBlob, out ReadOnlyMemory<byte> signature)
    {
        signature = default;
        var reader = new SshAgentPayloadReader(signatureBlob);
        if (!reader.TryReadString(out var algorithm) || !algorithm.Span.SequenceEqual("ssh-ed25519"u8) ||
            !reader.TryReadString(out var signatureBytes) ||
            signatureBytes.Length != Ed25519.SignatureSize ||
            !reader.End)
        {
            return false;
        }

        signature = signatureBytes;
        return true;
    }
}
