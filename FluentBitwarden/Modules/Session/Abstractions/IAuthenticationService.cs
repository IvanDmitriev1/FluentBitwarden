using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Models.Authentication;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface IAuthenticationService
{
    Task<PasswordSignInOutcome> SignInWithPasswordAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default);

    Task<PasswordSignInOutcome> ContinueTwoFactorAsync(
        BitwardenClientContext context,
        PasswordSignInContinuation passwordSignInContinuation,
        TwoFactorProof twoFactorProof,
        CancellationToken cancellationToken);
}