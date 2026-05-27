using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Session.Models;

public abstract record AccountLoginRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.SignIn;

    public sealed record PasswordRequest(
        BitwardenClientContext Context,
        string Email,
        string MasterPassword) : AccountLoginRequest;

    public sealed record PasskeyRequest(BitwardenClientContext Context) : AccountLoginRequest;

    public sealed record TwoFactorRequest(
        BitwardenClientContext Context,
        string Email,
        string ServerAuthorizationHash,
        TwoFactorProof TwoFactorProof) : AccountLoginRequest;
}
