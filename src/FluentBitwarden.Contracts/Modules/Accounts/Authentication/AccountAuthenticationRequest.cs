using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.Contracts.Modules.Accounts.Authentication;

[MemoryPackable]
[MemoryPackUnion(0, typeof(Password))]
[MemoryPackUnion(1, typeof(Passkey))]
[MemoryPackUnion(2, typeof(TwoFactor))]
public abstract partial record AccountAuthenticationRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.LogIn;

    [MemoryPackable]
    public sealed partial record Password(
        BitwardenClientContext Context,
        string Email,
        string MasterPassword) : AccountAuthenticationRequest;

    [MemoryPackable]
    public sealed partial record Passkey(BitwardenClientContext Context, NativeWindowHandle OwerHwnd) : AccountAuthenticationRequest;

    [MemoryPackable]
    public sealed partial record TwoFactor(
        BitwardenClientContext Context,
        string Email,
        string ServerAuthorizationHash,
        IdentityTwoFactorProof TwoFactorProof) : AccountAuthenticationRequest;
}
