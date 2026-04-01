using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record SessionTokens(
    RefreshToken RefreshToken,
    TwoFactorToken? TwoFactorToken,
    AccessToken AccessToken,
    DateTimeOffset ExpiresAt);