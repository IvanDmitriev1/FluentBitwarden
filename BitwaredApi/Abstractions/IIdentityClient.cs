using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

public interface IIdentityClient
{
    ValueTask<PreloginResponseModel> PreloginAsync(string email, CancellationToken cancellationToken = default);

    ValueTask<TokenResponseModel> ExchangePasswordAsync(
        PasswordTokenRequestModel request,
        CancellationToken cancellationToken = default);

    ValueTask<TokenResponseModel> RefreshTokenAsync(
        RefreshTokenRequestModel request,
        CancellationToken cancellationToken = default);
}
