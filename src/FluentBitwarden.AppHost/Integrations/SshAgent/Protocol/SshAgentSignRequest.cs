namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal readonly record struct SshAgentSignRequest(
    ReadOnlyMemory<byte> PublicKeyBlob,
    ReadOnlyMemory<byte> Data,
    SshAgentSignatureFlags Flags)
{
    public static bool TryParse(ReadOnlyMemory<byte> payload, out SshAgentSignRequest request)
    {
        request = default;
        var reader = new SshAgentPayloadReader(payload);

        if (!reader.TryReadString(out var publicKeyBlob) ||
            !reader.TryReadString(out var data) ||
            !reader.TryReadUInt32(out var flags) ||
            !reader.End)
        {
            return false;
        }

        request = new SshAgentSignRequest(
            publicKeyBlob,
            data,
            (SshAgentSignatureFlags)flags);

        return true;
    }
}
