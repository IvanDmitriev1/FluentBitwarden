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

}
