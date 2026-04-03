namespace BitwardenApi.Modules.Identity.Abstractions;

public interface IIdentityApiClient
{
    Task<TokenExchangeOutcome> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenExchangeOutcome> LoginWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenExchangeOutcome> RefreshAsync(
        RefreshLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenExchangeOutcome> LoginWithDeviceAsync(
        DeviceLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenExchangeOutcome> LoginWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default);
}
