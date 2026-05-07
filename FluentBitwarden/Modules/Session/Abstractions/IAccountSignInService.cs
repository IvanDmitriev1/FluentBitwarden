using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountSignInService
{
    Task<AccountSignInOutcome> SignInWithPasswordAsync(
        AccountSignInRequest.PasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountSignInOutcome> SignInWithTwoFactorAsync(
        AccountSignInRequest.TwoFactorRequest request,
        CancellationToken cancellationToken);
}