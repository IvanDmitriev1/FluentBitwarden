using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Vault.Ciphers.Responses;
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
    public async Task Get_revision_date_accepts_the_unix_epoch_boundary()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("0"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        DateTimeOffset result = await provider.GetRequiredService<IVaultSyncApi>().GetRevisionDateAsync(
            TestApiSupport.AccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(DateTimeOffset.UnixEpoch, result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Get_revision_date_accepts_the_maximum_supported_unix_millisecond_value()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("253402300799999"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        DateTimeOffset result = await provider.GetRequiredService<IVaultSyncApi>().GetRevisionDateAsync(
            TestApiSupport.AccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero), result);
    }

    [Fact]
    public async Task Get_revision_date_rejects_values_above_the_datetime_offset_range()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("253402300800000"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IVaultSyncApi>()
            .GetRevisionDateAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Get_revision_date_throws_for_a_failed_server_status()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("unavailable", HttpStatusCode.Forbidden));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(() => provider
            .GetRequiredService<IVaultSyncApi>()
            .GetRevisionDateAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Single(handler.Requests);
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

    [Fact]
    public async Task Get_sync_maps_organization_collection_and_cipher_encrypted_fields()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        const string asymmetricEncoded = "3.AQID";
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                $$"""
                {
                  "profile": {
                    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "name": "Test User",
                    "email": "user@example.test",
                    "culture": "en-US",
                    "creationDate": "2026-01-01T00:00:00Z",
                    "organizations": [{
                      "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                      "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                      "organizationUserId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
                      "name": "Example Organization",
                      "status": 2,
                      "enabled": 1,
                      "accessSecretsManager": false,
                      "key": "{{asymmetricEncoded}}",
                      "customOrganizationSetting": "preserved"
                    }],
                    "theme": "dark"
                  },
                  "folders": [],
                  "collections": [{
                    "id": "collection-id",
                    "organizationId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                    "readOnly": 1,
                    "manage": false,
                    "hidePasswords": 0,
                    "type": 2,
                    "name": "{{encoded}}"
                  }],
                  "ciphers": [{
                    "id": "cipher-id",
                    "type": 5,
                    "key": "{{encoded}}",
                    "revisionDate": "2026-01-02T00:00:00Z",
                    "creationDate": "2026-01-01T00:00:00Z",
                    "favorite": 1,
                    "reprompt": 0,
                    "edit": true,
                    "viewPassword": false,
                    "data": "{\"sshKey\":\"encrypted\"}"
                  }]
                }
                """));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        VaultSyncResponse result = await provider.GetRequiredService<IVaultSyncApi>().GetSyncAsync(
            TestApiSupport.AccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal("dark", result.Profile.Settings["theme"].GetString());
        VaultOrganizationResponse organization = Assert.Single(result.Profile.Organizations);
        Assert.Equal(OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), organization.Id);
        Assert.Equal(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), organization.UserId);
        Assert.Equal(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), organization.OrganizationUserId);
        Assert.True(organization.Enabled);
        Assert.False(organization.AccessSecretsManager);
        Assert.Equal(
            AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
                EncryptionType.Rsa2048_OaepSha256_B64,
                ReadOnlySpan<byte>.Empty,
                [1, 2, 3],
                ReadOnlySpan<byte>.Empty)),
            organization.ProtectedOrganizationKey);
        Assert.Equal("preserved", organization.Settings["customOrganizationSetting"].GetString());
        var collection = Assert.Single(result.Collections);
        Assert.Equal(CollectionId.Parse("collection-id"), collection.Id);
        Assert.Equal(OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), collection.OrganizationId);
        Assert.True(collection.ReadOnly);
        Assert.False(collection.Manage);
        Assert.False(collection.HidePasswords);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), collection.EncryptedName);
        CipherResponse cipher = Assert.Single(result.VaultCiphers);
        Assert.Equal(CipherType.SshKey, cipher.VaultCipherType);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), cipher.ProtectedCipherKey);
        Assert.True(cipher.Favorite);
        Assert.False(cipher.Reprompt);
        Assert.True(cipher.Edit);
        Assert.False(cipher.ViewPassword);
        Assert.Equal("{\"sshKey\":\"encrypted\"}"u8.ToArray(), cipher.Data);
        Assert.Equal("excludeDomains=true", Assert.Single(handler.Requests).RequestUri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task Get_sync_throws_for_failed_status_and_null_response_payload()
    {
        using var failedHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("maintenance", HttpStatusCode.Forbidden));
        using (ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: failedHandler))
        {
            BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(() => provider
                .GetRequiredService<IVaultSyncApi>()
                .GetSyncAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
            Assert.Single(failedHandler.Requests);
        }

        using var nullHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("null"));
        using ServiceProvider nullProvider = TestApiSupport.CreateProvider(vaultHandler: nullHandler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => nullProvider
            .GetRequiredService<IVaultSyncApi>()
            .GetSyncAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));
        Assert.Single(nullHandler.Requests);
    }

    [Fact]
    public async Task Sync_cancellation_before_send_does_not_reach_transport()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IVaultSyncApi>()
            .GetSyncAsync(TestApiSupport.AccountContext, cancellation.Token));

        Assert.Empty(handler.Requests);
    }

}
