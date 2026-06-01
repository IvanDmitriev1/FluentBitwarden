namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public readonly partial record struct PasskeyGetAssertionRequest(
    string RpId,
    byte[] RpIdHash,
    byte[] ClientDataHash) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Passkey.GetAssertion;
}
