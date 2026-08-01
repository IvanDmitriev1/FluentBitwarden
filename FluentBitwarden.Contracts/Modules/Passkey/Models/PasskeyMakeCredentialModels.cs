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
public sealed partial class PasskeyMakeCredentialResponse
{
    public required byte[] CredentialId { get; init; }
    public required byte[] AttestationObject { get; init; } // fmt=none, full CBOR
    public required byte[] AuthenticatorData { get; init; }  // for AddCredentials cache
    public required byte[] CredentialPublicKeyCose { get; init; } // for cache metadata
    public required string RpId { get; init; }
    public required string UserName { get; init; }
}
