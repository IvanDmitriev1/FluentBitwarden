using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Models.Authentication;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface IAuthenticationService
{
    Task<PasswordSignInOutcome> SignInWithPasswordAsync(
        BitwardenClientContext context,
        string email,
        string masterPassword,
        CancellationToken cancellationToken = default);

    Task<PasswordSignInOutcome> ContinueTwoFactorAsync(
        BitwardenClientContext context,
        string email,
        string serverAuthorizationHash,
        TwoFactorProof twoFactorProof,
        CancellationToken cancellationToken);
}
