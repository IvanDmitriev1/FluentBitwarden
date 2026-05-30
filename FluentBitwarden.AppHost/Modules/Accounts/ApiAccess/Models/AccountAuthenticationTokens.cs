using BitwardenApi.Models;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Contracts.Shared;

namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models;

public sealed record AccountAuthenticationTokens(
    UserId UserId,
    BitwardenClientContext BitwardenClientContext,
    RefreshToken RefreshToken,
    AccessToken AccessToken,
    DateTimeOffset ExpiresAt)
{
    public static AccountAuthenticationTokens Create(AccountProfile accountProfile, RefreshToken token) =>
        new(accountProfile.UserId,
            new BitwardenClientContext(accountProfile.Environment, DeviceIdentity.DeviceInfo),
            token,
            AccessToken.Empty,
            DateTimeOffset.MinValue);

    public bool IsValid() =>
        RefreshToken != RefreshToken.Empty &&
        AccessToken != AccessToken.Empty &&
        ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
}