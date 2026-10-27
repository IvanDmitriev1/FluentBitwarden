using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Vault.Attachments;

namespace BitwardenApi.Tests.Tests;

public sealed class AttachmentsApiTests
{
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
        CipherId cipherId = CipherId.Parse("cipher-id");
        AttachmentId attachmentId = AttachmentId.Parse("attachment-id");
        byte[]? downloaded = null;
        EncString? encryptedKey = null;

        await provider.GetRequiredService<IAttachmentsApi>().DownloadToAsync(
            TestApiSupport.AccountContext,
            cipherId,
            attachmentId,
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
        CipherId cipherId = CipherId.Parse("cipher-id");
        AttachmentId attachmentId = AttachmentId.Parse("attachment-id");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider
            .GetRequiredService<IAttachmentsApi>()
            .DownloadToAsync(
                TestApiSupport.AccountContext,
                cipherId,
                attachmentId,
                static (_, _) => Task.CompletedTask,
                TestContext.Current.CancellationToken));

        Assert.Single(vaultHandler.Requests);
        Assert.Empty(blobHandler.Requests);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Download_attachment_rejects_an_empty_id_before_sending_any_request(bool emptyCipherId)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = emptyCipherId ? default : CipherId.Parse("cipher-id");
        AttachmentId attachmentId = emptyCipherId ? AttachmentId.Parse("attachment-id") : default;

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetRequiredService<IAttachmentsApi>()
            .DownloadToAsync(
                TestApiSupport.AccountContext,
                cipherId,
                attachmentId,
                static (_, _) => Task.CompletedTask,
                TestContext.Current.CancellationToken));

        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Download_attachment_stops_at_the_failed_metadata_or_blob_request(bool failBlobRequest)
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var vaultHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                $$"""
                { "id": "attachment-id", "url": "https://blob.bitwarden.test/file", "fileName": "{{encoded}}", "key": "{{encoded}}", "size": 4 }
                """,
                failBlobRequest ? HttpStatusCode.OK : HttpStatusCode.NotFound));
        using var blobHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("storage access denied", HttpStatusCode.Forbidden));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: vaultHandler, attachmentHandler: blobHandler);

        BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(() => provider
            .GetRequiredService<IAttachmentsApi>()
            .DownloadToAsync(
                TestApiSupport.AccountContext,
                CipherId.Parse("cipher-id"),
                AttachmentId.Parse("attachment-id"),
                static (_, _) => Task.CompletedTask,
                TestContext.Current.CancellationToken));

        Assert.Equal(failBlobRequest ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Single(vaultHandler.Requests);
        Assert.Equal(["Bearer vault-access-token"], vaultHandler.Requests[0].Headers["Authorization"]);
        Assert.Equal(failBlobRequest ? 1 : 0, blobHandler.Requests.Count);
        if (failBlobRequest)
            Assert.DoesNotContain("Authorization", blobHandler.Requests[0].Headers.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Download_attachment_propagates_cancellation_from_the_stream_handler()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var vaultHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                $$"""
                { "id": "attachment-id", "url": "https://blob.bitwarden.test/file", "fileName": "{{encoded}}", "key": "{{encoded}}", "size": 4 }
                """));
        using var blockingStream = new AttachmentBlockingReadStream();
        using var blobHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(blockingStream) });
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: vaultHandler, attachmentHandler: blobHandler);
        using var cancellation = new CancellationTokenSource();
        int streamHandlerCalls = 0;
        Task download = provider.GetRequiredService<IAttachmentsApi>().DownloadToAsync(
            TestApiSupport.AccountContext,
            CipherId.Parse("cipher-id"),
            AttachmentId.Parse("attachment-id"),
            async (stream, _) =>
            {
                Interlocked.Increment(ref streamHandlerCalls);
                await stream.CopyToAsync(Stream.Null, cancellation.Token);
            },
            cancellation.Token);

        await blockingStream.ReadStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => download);
        Assert.Equal(1, streamHandlerCalls);
        Assert.Single(vaultHandler.Requests);
        Assert.Single(blobHandler.Requests);
    }

}
