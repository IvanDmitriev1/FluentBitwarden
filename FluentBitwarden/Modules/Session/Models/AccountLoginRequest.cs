using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountLoginRequest
{
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
