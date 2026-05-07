using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record AccountSessionTokens(
    RefreshToken RefreshToken,
    AccessToken AccessToken,
    DateTimeOffset ExpiresAt)
{
    public static AccountSessionTokens Create(RefreshToken token) =>
        new(token, AccessToken.Empty, DateTimeOffset.MinValue);

    public bool IsValid() =>
        RefreshToken != RefreshToken.Empty &&
        AccessToken != AccessToken.Empty &&
        ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
}