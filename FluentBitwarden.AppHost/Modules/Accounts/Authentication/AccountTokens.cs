using FluentBitwarden.Contracts.Infrastructure;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

internal sealed record AccountTokens(
    UserId UserId,
    BitwardenClientContext BitwardenClientContext,
    RefreshToken RefreshToken,
    AccessToken AccessToken,
    DateTimeOffset ExpiresAt)
{
    public static AccountTokens Create(AccountProfile accountProfile, RefreshToken token) =>
        new(
            accountProfile.UserId,
            new BitwardenClientContext(accountProfile.Environment, DeviceIdentity.DeviceInfo),
            token,
            AccessToken.Empty,
            DateTimeOffset.MinValue);

    public bool IsValid() =>
        RefreshToken != RefreshToken.Empty &&
        AccessToken != AccessToken.Empty &&
        ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
}
