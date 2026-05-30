using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Login;

using AccountLoginOperationResult = OperationResult<AccountLoginOutcome, AccountLoginSuccess>;

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