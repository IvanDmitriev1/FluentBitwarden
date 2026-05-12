using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountLoginService
{
    Task<AccountLoginnOutcome> LoginWithPasswordAsync(
        AccountLoginRequest.PasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountLoginnOutcome> LoginWithPasskeyAsync(
        AccountLoginRequest.PasskeyRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountLoginnOutcome> LoginWithTwoFactorAsync(
        AccountLoginRequest.TwoFactorRequest request,
        CancellationToken cancellationToken);
}
