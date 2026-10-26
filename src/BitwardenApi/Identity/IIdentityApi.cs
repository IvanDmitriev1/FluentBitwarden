using BitwardenApi.Identity.Models;

namespace BitwardenApi.Identity;

public interface IIdentityApi
{
    Task<SessionTokenResult<AuthenticatedSession>> AuthenticateWithPasswordAsync(
        PasswordAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<AuthenticatedSession>> AuthenticateWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<AuthenticatedSession>> AuthenticateWithWebAuthnAsync(
        WebAuthnAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<RefreshedSession>> RefreshAuthenticationAsync(
        RefreshAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<AuthenticatedSession>> AuthenticateWithDeviceAsync(
        DeviceAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult<AuthenticatedSession>> AuthenticateWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default);
}
