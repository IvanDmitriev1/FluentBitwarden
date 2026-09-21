using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Infrastructure.Data;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal static class AccountProfileMapper
{
    public static AccountProfile ToDomain(Row row) =>
        new(
            UserId.Parse(row.UserId),
            row.Email,
            new BitwardenEnvironment(
                new Uri(row.ApiBase, UriKind.Absolute),
                new Uri(row.IdentityBase, UriKind.Absolute),
                new Uri(row.NotificationsBase, UriKind.Absolute),
                new Uri(row.VaultBase, UriKind.Absolute)));

    public static AccountProfile? ToDomainOrNull(Row? row) => row is null ? null : ToDomain(row);

    public static UserIdParameters ToUserIdParameters(UserId userId) => new(userId.ToString());

    public static AccountProfileDetails? ToDetails(DetailsRow? row)
    {
        if (row is null || row.ProfileSynced == 0 || row.ProfileName is null || row.ProfileCulture is null ||
            row.ProfileCreationDateUnixMs is not { } creationDateUnixMs)
        {
            return null;
        }

        return new AccountProfileDetails(
            row.ProfileName,
            row.ProfileCulture,
            creationDateUnixMs.ToDateTimeOffsetFromUnixMs());
    }

    public static SyncedProfileParameters ToSyncedProfileParameters(
        UserId accountId,
        VaultProfileResponse profile) => new(
        profile.Email,
        profile.Name,
        profile.Culture,
        profile.CreationDate.ToUnixMs(),
        accountId.ToString());

    public static UpsertParameters ToUpsertParameters(AccountProfile accountProfile) => new(
        accountProfile.UserId.ToString(),
        accountProfile.Email,
        accountProfile.Environment.ApiBase.ToString(),
        accountProfile.Environment.IdentityBase.ToString(),
        accountProfile.Environment.NotificationsBase.ToString(),
        accountProfile.Environment.VaultBase.ToString());

    internal sealed class Row
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiBase { get; set; } = string.Empty;
        public string IdentityBase { get; set; } = string.Empty;
        public string NotificationsBase { get; set; } = string.Empty;
        public string VaultBase { get; set; } = string.Empty;
    }

    internal sealed class DetailsRow
    {
        public string? ProfileName { get; set; }
        public string? ProfileCulture { get; set; }
        public long? ProfileCreationDateUnixMs { get; set; }
        public int ProfileSynced { get; set; }
    }

    internal sealed record SyncedProfileParameters(
        string Email,
        string ProfileName,
        string ProfileCulture,
        long ProfileCreationDateUnixMs,
        string UserId);

    internal sealed record UpsertParameters(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        string VaultBase);

    internal sealed record UserIdParameters(string UserId);
}
