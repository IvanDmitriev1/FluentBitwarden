namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public sealed partial record PasskeyMakeCredentialRequest(
    string RpId,
    string RpName,
    byte[] RpIdHash,          // 32
    byte[] ClientDataHash,    // 32
    byte[] UserId,
    string UserName,
    string UserDisplayName,
    bool RequireResidentKey,
    bool UserVerification) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Passkey.MakeCredential;
}

[MemoryPackable]
public sealed partial record PasskeyMakeCredentialResponse
{
    public required byte[] AuthenticatorData { get; init; }
}
