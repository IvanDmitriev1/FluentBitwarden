using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

internal interface IIdentityClient
{
    ValueTask<PreloginResponseModel> PreloginAsync(
        BitwardenEnvironment environment,
        string email,
        CancellationToken cancellationToken = default);

    ValueTask<TokenExchangeOutcome> ExchangePasswordAsync(
        BitwardenEnvironment environment,
        PasswordTokenRequestModel request,
        CancellationToken cancellationToken = default);

    ValueTask<TokenExchangeOutcome> RefreshTokenAsync(
        BitwardenEnvironment environment,
        RefreshTokenRequestModel request,
        CancellationToken cancellationToken = default);
}
