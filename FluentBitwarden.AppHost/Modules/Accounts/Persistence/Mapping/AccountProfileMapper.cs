using FluentBitwarden.AppHost.Data.Mapping;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence.Mapping;

internal static class AccountProfileMapper
{
    public sealed record AccountProfileRow(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        string VaultBase);

    public sealed record AccountProfileDetailsRow(
        string? ProfileName,
        string? ProfileCulture,
        long? ProfileCreationDateUnixMs,
        int ProfileSynced);

    public static AccountProfile ToDomain(AccountProfileRow row) => new(
        UserId: UserId.Parse(row.UserId),
        Email: row.Email,
        Environment: new BitwardenEnvironment(
            ApiBase: new Uri(row.ApiBase, UriKind.Absolute),
            IdentityBase: new Uri(row.IdentityBase, UriKind.Absolute),
            NotificationsBase: new Uri(row.NotificationsBase, UriKind.Absolute),
            VaultBase: new Uri(row.VaultBase, UriKind.Absolute)));

    public static AccountProfileDetails? ToProfileDetails(AccountProfileDetailsRow row)
    {
        if (row.ProfileSynced == 0)
            return null;

        return new AccountProfileDetails(
            Name: row.ProfileName ?? string.Empty,
            Culture: row.ProfileCulture ?? string.Empty,
            CreationDate: row.ProfileCreationDateUnixMs.ToDateTimeOffsetFromUnixMs() ?? DateTimeOffset.UnixEpoch);
    }

    public readonly record struct UpsertParameters(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        string VaultBase);

    public static UpsertParameters ToUpsertParameters(AccountProfile accountProfile) => new(
        UserId: accountProfile.UserId.ToString(),
        Email: accountProfile.Email,
        ApiBase: accountProfile.Environment.ApiBase.ToString(),
        IdentityBase: accountProfile.Environment.IdentityBase.ToString(),
        NotificationsBase: accountProfile.Environment.NotificationsBase.ToString(),
        VaultBase: accountProfile.Environment.VaultBase.ToString());

    public readonly record struct UpdateSyncedParameters(
        string UserId,
        string Email,
        string? ProfileName,
        string? ProfileCulture,
        long ProfileCreationDateUnixMs);

    public static UpdateSyncedParameters ToUpdateSyncedParameters(UserId userId, VaultProfileResponse profile) => new(
        UserId: userId.ToString(),
        Email: profile.Email,
        ProfileName: NullIfWhiteSpace(profile.Name),
        ProfileCulture: NullIfWhiteSpace(profile.Culture),
        ProfileCreationDateUnixMs: profile.CreationDate.ToUnixMs());

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
