using FluentBitwarden.AppHost.Modules.SshAgent.Internal;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Models;

public readonly record struct SshAgentPacket(
    SshAgentMessageRequests Message,
    ReadOnlyMemory<byte> Payload);

public readonly record struct SshPublicIdentityResponce(
    byte[] PublicKey,
    string Comment);

public readonly record struct SshAgentExtensionRequest(
    ReadOnlyMemory<byte> ExtensionType,
    ReadOnlyMemory<byte> Payload)
{
    public static SshAgentExtensionRequest Parse(ReadOnlyMemory<byte> payload)
    {
        var reader = new SshAgentPayloadReader(payload);

        var extensionType = reader.ReadString();
        var extensionPayload = payload[^reader.Remaining..];

        return new SshAgentExtensionRequest(extensionType, extensionPayload);
    }
}

internal readonly record struct SshSignRequest(
    ReadOnlyMemory<byte> PublicKeyBlob,
    ReadOnlyMemory<byte> Data,
    SshAgentSignatureFlags Flags)
{
    public static SshSignRequest Parse(ReadOnlyMemory<byte> payload)
    {
        var reader = new SshAgentPayloadReader(payload);

        ReadOnlyMemory<byte> publicKeyBlob = reader.ReadString();
        ReadOnlyMemory<byte> data = reader.ReadString();
        int rawFlags = reader.ReadUInt32();

        if (!reader.End)
            throw new ArgumentException("Unexpected bytes while parsing SshSignRequest");

        return new SshSignRequest(
            publicKeyBlob,
            data,
            (SshAgentSignatureFlags)rawFlags);
    }
}

internal readonly record struct SshSignatureResult(
    string AlgorithmName,
    byte[] Signature)
{
    public static SshSignatureResult Failed { get; } = new(string.Empty, []);
}