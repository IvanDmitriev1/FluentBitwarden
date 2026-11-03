namespace BitwardenApi.Tests.Tests.Client;

public sealed class BitwardenEnvironmentTests
{
    [Theory]
    [InlineData("https://api.test", "/")]
    [InlineData("https://api.test/api", "/api/")]
    [InlineData("https://api.test/api/", "/api/")]
    [InlineData("https://api.test/tenant/v1/api", "/tenant/v1/api/")]
    public void Constructor_normalizes_service_bases_as_directories(
        string apiBase,
        string expectedApiPath)
    {
        var environment = new BitwardenEnvironment(
            new Uri(apiBase),
            new Uri("https://identity.test/identity"),
            new Uri("https://notifications.test/notifications"),
            new Uri("https://vault.test/vault"));

        Assert.Equal(expectedApiPath, environment.ApiBase.AbsolutePath);
        Assert.Equal("/identity/", environment.IdentityBase.AbsolutePath);
        Assert.Equal("/notifications/", environment.NotificationsBase.AbsolutePath);
        Assert.Equal("/vault", environment.VaultBase.AbsolutePath);

        Uri syncEndpoint = new(environment.ApiBase, "sync?excludeDomains=true");
        Assert.Equal($"{expectedApiPath}sync", syncEndpoint.AbsolutePath);
        Assert.Equal("?excludeDomains=true", syncEndpoint.Query);
    }

    [Fact]
    public void With_updates_re_normalize_changed_service_bases_without_stale_paths()
    {
        BitwardenEnvironment environment = BitwardenEnvironment.UnitedStates with
        {
            ApiBase = new Uri("https://api.test/tenant/v2/api"),
            IdentityBase = new Uri("https://identity.test/tenant/identity"),
            NotificationsBase = new Uri("https://notifications.test/tenant/notifications"),
            VaultBase = new Uri("https://vault.test/custom")
        };

        Assert.Equal("/tenant/v2/api/sync", new Uri(environment.ApiBase, "sync").AbsolutePath);
        Assert.Equal(
            "/tenant/identity/connect/token",
            new Uri(environment.IdentityBase, "connect/token").AbsolutePath);
        Assert.Equal("/tenant/notifications/hub", new Uri(environment.NotificationsBase, "hub").AbsolutePath);
        Assert.Equal("/custom", environment.VaultBase.AbsolutePath);
    }
}
