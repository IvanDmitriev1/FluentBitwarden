

using FluentBitwarden.Platform.Infrastructure;

namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

internal sealed record AccountTokens(
    UserId UserId,
    BitwardenClientContext BitwardenClientContext,
    RefreshToken RefreshToken,
    AccessToken AccessToken,
    DateTimeOffset ExpiresAt)
{
    public static AccountTokens Create(
        BitwardenAccountContext accountContext,
        RefreshToken token) =>
        new(
            accountContext.UserId,
            new BitwardenClientContext(accountContext.Environment, DeviceIdentity.DeviceInfo),
            token,
            AccessToken.Empty,
            DateTimeOffset.UnixEpoch);

    public bool IsFor(BitwardenAccountContext accountContext) =>
        UserId == accountContext.UserId &&
        BitwardenClientContext.Environment == accountContext.Environment;

    public bool IsValid() =>
        RefreshToken != RefreshToken.Empty &&
        AccessToken != AccessToken.Empty &&
        ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
}
