using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountSignInRequest;

public sealed record AccountSignInWithPasswordRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPassword) : AccountSignInRequest;

public sealed record AccountSignInWithTwoFactorRequest(
    BitwardenClientContext Context,
    string Email, 
    string ServerAuthorizationHash,
    TwoFactorProof TwoFactorProof) : AccountSignInRequest;