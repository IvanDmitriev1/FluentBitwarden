namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public readonly partial record struct PasskeyGetAssertionRequest(
    string RpId,
    byte[] RpIdHash,
    byte[] ClientDataHash) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Passkey.GetAssertion;
}


[MemoryPackable]
public sealed partial class PasskeyAssertionResponse
{
    public required byte[] CredentialId { get; init; }
    public required byte[] UserId { get; init; }
    public required byte[] AuthenticatorData { get; init; }
    public required byte[] Signature { get; init; }
    public required string UserName { get; init; }
    public required string UserDisplayName { get; init; }
}
