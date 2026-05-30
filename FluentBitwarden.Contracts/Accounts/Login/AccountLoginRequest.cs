using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(PasswordRequest))]
[MemoryPackUnion(1, typeof(PasskeyRequest))]
[MemoryPackUnion(2, typeof(TwoFactorRequest))]
public abstract partial record AccountLoginRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.LogIn;

    [MemoryPackable]
    public sealed partial record PasswordRequest(
        BitwardenClientContext Context,
        string Email,
        string MasterPassword) : AccountLoginRequest;

    [MemoryPackable]
    public sealed partial record PasskeyRequest(BitwardenClientContext Context, IntPtr OwerHwnd) : AccountLoginRequest;

    [MemoryPackable]
    public sealed partial record TwoFactorRequest(
        BitwardenClientContext Context,
        string Email,
        string ServerAuthorizationHash,
        TwoFactorProof TwoFactorProof) : AccountLoginRequest;
}
