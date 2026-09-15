namespace BitwardenApi.Identity;

public interface IIdentityApi
{
    Task<TokenExchangeOutcome> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenExchangeOutcome> LoginWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<WebAuthnLoginAssertionOptionsResult> GetWebAuthnLoginAssertionOptionsAsync(
        BitwardenClientContext context,
        CancellationToken cancellationToken = default);

    Task<TokenExchangeOutcome> LoginWithWebAuthnAsync(
        WebAuthnLoginRequest request,
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
