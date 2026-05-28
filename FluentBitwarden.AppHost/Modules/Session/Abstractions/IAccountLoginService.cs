using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

using AccountLoginOperationResult = OperationResult<AccountLoginOutcome, AccountSignInSuccess>;

internal interface IAccountLoginService
{
    Task<AccountLoginOperationResult> LoginWithPasswordAsync(
        AccountLoginRequest.PasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountLoginOperationResult> LoginWithPasskeyAsync(
        AccountLoginRequest.PasskeyRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountLoginOperationResult> LoginWithTwoFactorAsync(
        AccountLoginRequest.TwoFactorRequest request,
        CancellationToken cancellationToken);
}
