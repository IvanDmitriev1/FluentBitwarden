using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountSignInRequest
{
    public sealed record PasswordRequest(
        BitwardenClientContext Context,
        string Email,
        string MasterPassword) : AccountSignInRequest;

    public sealed record TwoFactorRequest(
        BitwardenClientContext Context,
        string Email,
        string ServerAuthorizationHash,
        TwoFactorProof TwoFactorProof) : AccountSignInRequest;
}
