using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Vault.Attachments;
using BitwardenApi.Vault.Attachments.Contracts;
using BitwardenApi.Vault.Items;
using BitwardenApi.Vault.Items.Contracts;

namespace BitwardenApi.Tests.Tests;

public sealed class VaultApiTests
{
    [Fact]
    public async Task Get_revision_date_returns_the_server_epoch_milliseconds_from_an_authenticated_request()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("1767225600123"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        DateTimeOffset result = await provider.GetRequiredService<IVaultItemsApi>().GetRevisionDateAsync(
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
            .GetRequiredService<IVaultItemsApi>()
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

        VaultSyncResponse result = await provider.GetRequiredService<IVaultItemsApi>().GetSyncAsync(
            TestApiSupport.AccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), result.Profile.Id);
        Assert.Equal("user@example.test", result.Profile.Email);
        VaultFolderResponse folder = Assert.Single(result.Folders);
        Assert.Equal(FolderId.Parse("folder-id"), folder.Id);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T00:00:00Z"), folder.RevisionDate);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), folder.EncryptedName);
        Assert.Equal("/sync", Assert.Single(handler.Requests).RequestUri.AbsolutePath);
        Assert.Equal("excludeDomains=true", handler.Requests.Single().RequestUri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task Download_attachment_authenticates_metadata_without_leaking_credentials_to_the_presigned_blob_url()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var vaultHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse($$"""
                {
                  "id": "attachment-id",
                  "url": "https://blob.bitwarden.test/download?signature=abc",
                  "fileName": "{{encoded}}",
                  "key": "{{encoded}}",
                  "size": 4
                }
                """));
        using var blobHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.BytesResponse("blob"u8.ToArray()));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: vaultHandler, attachmentHandler: blobHandler);
        var attachment = new VaultCipherAttachment
        {
            CipherId = CipherId.Parse("cipher-id"),
            Id = AttachmentId.Parse("attachment-id")
        };
        byte[]? downloaded = null;
        EncString? encryptedKey = null;

        await provider.GetRequiredService<IVaultCipherAttachmentApi>().DownloadToAsync(
            TestApiSupport.AccountContext,
            attachment,
            async (stream, key) =>
            {
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory, TestContext.Current.CancellationToken);
                downloaded = memory.ToArray();
                encryptedKey = key;
            },
            TestContext.Current.CancellationToken);

        TestApiSupport.RecordedRequest metadataRequest = Assert.Single(vaultHandler.Requests);
        Assert.Equal("/ciphers/cipher-id/attachment/attachment-id", metadataRequest.RequestUri.AbsolutePath);
        Assert.Equal(["Bearer vault-access-token"], metadataRequest.Headers["Authorization"]);
        TestApiSupport.RecordedRequest blobRequest = Assert.Single(blobHandler.Requests);
        Assert.Equal("https://blob.bitwarden.test/download?signature=abc", blobRequest.RequestUri.AbsoluteUri);
        Assert.DoesNotContain("Authorization", blobRequest.Headers.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bitwarden-Client-Version", blobRequest.Headers.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Accept", blobRequest.Headers.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("blob"u8.ToArray(), downloaded);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), encryptedKey!.Value);
    }

    [Fact]
    public async Task Download_attachment_with_an_invalid_presigned_url_does_not_fetch_a_blob()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var vaultHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse($$"""
                { "id": "attachment-id", "url": "not a URL", "fileName": "{{encoded}}", "key": "{{encoded}}", "size": 4 }
                """));
        using var blobHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.BytesResponse([]));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: vaultHandler, attachmentHandler: blobHandler);
        var attachment = new VaultCipherAttachment
        {
            CipherId = CipherId.Parse("cipher-id"),
            Id = AttachmentId.Parse("attachment-id")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider
            .GetRequiredService<IVaultCipherAttachmentApi>()
            .DownloadToAsync(TestApiSupport.AccountContext, attachment, static (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken));

        Assert.Single(vaultHandler.Requests);
        Assert.Empty(blobHandler.Requests);
    }
}
