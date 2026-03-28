using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record SessionTokens(
    AccessToken AccessToken,
    RefreshToken RefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt);