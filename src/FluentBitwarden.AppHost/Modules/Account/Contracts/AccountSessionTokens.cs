using BitwardenApi.Primitives;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public sealed record AccountSessionTokens(
    UserId UserId,
    BitwardenClientContext ClientContext,
    SessionRefreshToken RefreshToken,
    SessionAccessToken AccessToken,
    DateTimeOffset ExpiresAt)
{
    public bool IsValid() =>
        RefreshToken != SessionRefreshToken.Empty &&
        AccessToken != SessionAccessToken.Empty &&
        ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
}
