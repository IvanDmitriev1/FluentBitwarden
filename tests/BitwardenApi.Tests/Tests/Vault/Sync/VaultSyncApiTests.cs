using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Vault.Sync;

namespace BitwardenApi.Tests.Tests;

public sealed class VaultSyncApiTests
{
    [Fact]
    public async Task Get_revision_date_returns_the_server_epoch_milliseconds_from_an_authenticated_request()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("1767225600123"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        DateTimeOffset result = await provider.GetRequiredService<IVaultSyncApi>().GetRevisionDateAsync(
            TestApiSupport.AccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1767225600123), result);
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/accounts/revision-date", request.RequestUri.AbsolutePath);
        Assert.Equal(["Bearer vault-access-token"], request.Headers["Authorization"]);
    }

    [Fact]
    public async Task Get_revision_date_rejects_a_negative_server_value()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("-1"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        await Assert.ThrowsAsync<BitwardenAuthorizationException>(() => provider
            .GetRequiredService<IVaultSyncApi>()
            .GetRevisionDateAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));

        Assert.Equal("/accounts/revision-date", Assert.Single(handler.Requests).RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task Get_sync_maps_a_representative_server_payload_and_excludes_domains()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse($$"""
                {
                  "profile": {
                    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "name": "Test User",
                    "email": "user@example.test",
                    "culture": "en-US",
                    "creationDate": "2026-01-01T00:00:00Z",
                    "organizations": []
                  },
                  "folders": [{
                    "id": "folder-id",
                    "revisionDate": "2026-01-02T00:00:00Z",
                    "name": "{{encoded}}"
                  }],
                  "collections": [],
                  "ciphers": []
                }
                """));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        VaultSyncResponse result = await provider.GetRequiredService<IVaultSyncApi>().GetSyncAsync(
            TestApiSupport.AccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), result.Profile.Id);
        Assert.Equal("user@example.test", result.Profile.Email);
        FolderResponse folder = Assert.Single(result.Folders);
        Assert.Equal(FolderId.Parse("folder-id"), folder.Id);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T00:00:00Z"), folder.RevisionDate);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), folder.EncryptedName);
        Assert.Equal("/sync", Assert.Single(handler.Requests).RequestUri.AbsolutePath);
        Assert.Equal("excludeDomains=true", handler.Requests.Single().RequestUri.Query.TrimStart('?'));
    }

}
