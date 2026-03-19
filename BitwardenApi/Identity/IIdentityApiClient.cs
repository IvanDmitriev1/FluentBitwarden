namespace BitwardenApi.Identity;

public interface IIdentityApiClient
{
    Task<TokenResponse> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenResponse> LoginWithPasswordAndTwoFactorAsync(
        PasswordTwoFactorLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenResponse> RefreshAsync(
        RefreshLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenResponse> LoginWithDeviceAsync(
        DeviceLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenResponse> LoginWithClientCredentialsAsync(
        ClientCredentialsLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenResponse> LoginWithAuthorizationCodeAsync(
        AuthorizationCodeLoginRequest request,
        CancellationToken cancellationToken = default);
}
