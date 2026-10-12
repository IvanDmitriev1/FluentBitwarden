namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal readonly record struct SshAgentExtensionRequest(
    ReadOnlyMemory<byte> ExtensionName,
    ReadOnlyMemory<byte> Data)
{
    public static bool TryParse(ReadOnlyMemory<byte> payload, out SshAgentExtensionRequest request)
    {
        request = default;
        var reader = new SshAgentPayloadReader(payload);

        if (!reader.TryReadString(out var extensionName))
            return false;

        request = new SshAgentExtensionRequest(extensionName, reader.ReadRemaining());

        return true;
    }
}
