using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountSignInService
{
    Task<AccountSignInOutcome> SignInWithPasswordAsync(
        AccountSignInWithPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountSignInOutcome> SignInWithTwoFactorAsync(
        AccountSignInWithTwoFactorRequest request,
        CancellationToken cancellationToken);
}