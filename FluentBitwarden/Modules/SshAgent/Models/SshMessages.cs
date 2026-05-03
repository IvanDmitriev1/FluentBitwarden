namespace FluentBitwarden.Modules.SshAgent.Models;

internal readonly record struct SshAgentPacket(
    SshAgentMessage Message,
    ReadOnlyMemory<byte> Payload);

internal readonly record struct SshPublicIdentity(
    OpenSshPublicKey PublicKey,
    string Comment);

internal readonly record struct SshSignRequest(
    ReadOnlyMemory<byte> PublicKeyBlob,
    ReadOnlyMemory<byte> Data,
    SshAgentSignFlags Flags);

internal readonly record struct SshSignatureResult(
    string AlgorithmName,
    ReadOnlyMemory<byte> RawSignature)
{
    public static SshSignatureResult Empty { get; } = new(string.Empty, ReadOnlyMemory<byte>.Empty);
};