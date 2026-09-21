namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal static class AccountTestData
{
    public const string FirstUserId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public const string SecondUserId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    public const string ThirdUserId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

    public static readonly DateTimeOffset ProfileCreationDate =
        new(2024, 5, 1, 0, 0, 0, TimeSpan.Zero);

    public static AccountProfile Profile(string userId, string email, string host)
    {
        var environment = new BitwardenEnvironment(
            new Uri($"https://api.{host}.test", UriKind.Absolute),
            new Uri($"https://identity.{host}.test", UriKind.Absolute),
            new Uri($"https://notifications.{host}.test", UriKind.Absolute),
            new Uri($"https://vault.{host}.test", UriKind.Absolute));

        return new AccountProfile(UserId.Parse(userId), email, environment);
    }

    public static VaultProfileResponse SyncedProfile(
        string userId,
        string email,
        string name,
        string culture,
        DateTimeOffset creationDate) => new()
        {
            Id = UserId.Parse(userId),
            Email = email,
            Name = name,
            Culture = culture,
            CreationDate = creationDate,
            Organizations = []
        };

    public static AccountKeyMaterial KeyMaterial(string userId, KdfConfig kdfConfig, string suffix) =>
        new(
            UserId.Parse(userId),
            $"dummy-salt-{suffix}",
            kdfConfig,
            ProtectedUserKey.Create(EncString.Encrypt($"dummy-user-key-{suffix}", new byte[64])),
            ProtectedPrivateKey.Create(EncString.Encrypt($"dummy-private-key-{suffix}", new byte[64])));
}
