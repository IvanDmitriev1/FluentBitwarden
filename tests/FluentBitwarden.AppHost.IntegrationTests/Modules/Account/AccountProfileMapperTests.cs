using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Persistance.Mappers;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountProfileMapperTests
{
    [Fact]
    public void ToSyncedProfileParameters_maps_profile_fields_and_creation_time_to_unix_milliseconds()
    {
        UserId userId = UserId.Parse(AccountTestData.FirstUserId);
        DateTimeOffset creationDate = new(2025, 6, 7, 8, 9, 10, 321, TimeSpan.FromHours(3));
        VaultProfileResponse profile = AccountTestData.SyncedProfile(
            userId.ToString(),
            "ada@example.test",
            "Ada Lovelace",
            "en-GB",
            creationDate);

        var result = AccountProfileMapper.ToSyncedProfileParameters(userId, profile);

        Assert.Equal(
            new AccountProfileMapper.SyncedProfileParameters(
                "ada@example.test",
                "Ada Lovelace",
                "en-GB",
                creationDate.ToUnixTimeMilliseconds(),
                userId.ToString()),
            result);
    }
}
