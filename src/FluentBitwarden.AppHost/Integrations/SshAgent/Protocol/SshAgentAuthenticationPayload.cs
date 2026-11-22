using System.Globalization;
using System.Text;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal readonly record struct SshAgentAuthenticationPayload(
    ReadOnlyMemory<byte> SessionId,
    string Username,
    bool IsHostBound,
    ReadOnlyMemory<byte> HostKeyBlob)
{
    private const int MaxSessionIdLength = 128;
    private const int MaxUsernameLength = 256;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static bool TryParse(
        ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> requestedPublicKeyBlob,
        out SshAgentAuthenticationPayload payload)
    {
        payload = default;
        var reader = new SshAgentPayloadReader(data);

        if (!reader.TryReadString(out var sessionId) || sessionId.IsEmpty || sessionId.Length > MaxSessionIdLength ||
            !reader.TryReadByte(out byte messageType) || messageType != 50 ||
            !reader.TryReadString(out var usernameBytes) || usernameBytes.IsEmpty || usernameBytes.Length > MaxUsernameLength ||
            !reader.TryReadString(out var service) || !service.Span.SequenceEqual("ssh-connection"u8) ||
            !reader.TryReadString(out var method) ||
            !reader.TryReadByte(out byte hasSignature) || hasSignature != 1 ||
            !reader.TryReadString(out var algorithm) ||
            !reader.TryReadString(out var publicKeyBlob) ||
            !publicKeyBlob.Span.SequenceEqual(requestedPublicKeyBlob.Span) ||
            !MatchesKeyAlgorithm(algorithm, publicKeyBlob))
        {
            return false;
        }

        bool isHostBound = method.Span.SequenceEqual("publickey-hostbound-v00@openssh.com"u8);
        if (!isHostBound && !method.Span.SequenceEqual("publickey"u8))
            return false;

        ReadOnlyMemory<byte> hostKeyBlob = default;
        if (isHostBound && (!reader.TryReadString(out hostKeyBlob) || hostKeyBlob.IsEmpty))
            return false;

        if (!reader.End || !TryReadUsername(usernameBytes, out string username))
            return false;

        payload = new SshAgentAuthenticationPayload(sessionId, username, isHostBound, hostKeyBlob);
        return true;
    }

    private static bool MatchesKeyAlgorithm(ReadOnlyMemory<byte> algorithm, ReadOnlyMemory<byte> publicKeyBlob)
    {
        var reader = new SshAgentPayloadReader(publicKeyBlob);
        return reader.TryReadString(out var keyType) && algorithm.Span.SequenceEqual(keyType.Span);
    }

    private static bool TryReadUsername(ReadOnlyMemory<byte> usernameBytes, out string username)
    {
        username = string.Empty;

        try
        {
            string decoded = StrictUtf8.GetString(usernameBytes.Span);
            if (decoded.Length > MaxUsernameLength || decoded.Contains('@'))
                return false;

            foreach (Rune rune in decoded.EnumerateRunes())
            {
                UnicodeCategory category = Rune.GetUnicodeCategory(rune);
                if (Rune.IsWhiteSpace(rune) || category is UnicodeCategory.Control or UnicodeCategory.Format)
                    return false;
            }

            username = decoded;
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
