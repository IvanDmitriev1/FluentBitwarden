namespace BitwardenApi.Identity;

public interface IIdentityApi
{
    Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithPasswordAsync(
        PasswordAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithWebAuthnAsync(
        WebAuthnAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<TokenRefreshSessionModel>> RefreshAuthenticationAsync(
        RefreshAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithDeviceAsync(
        DeviceAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<TokenAuthenticatedModel>> AuthenticateWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default);
}
