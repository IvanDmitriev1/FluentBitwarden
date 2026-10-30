using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Attachments.Responses;
using BitwardenApi.Vault.Ciphers;
using BitwardenApi.Vault.Ciphers.Responses;

namespace BitwardenApi.Tests.Tests;

public sealed class CiphersApiTests
{
    [Fact]
    public async Task Get_cipher_uses_authenticated_route_and_maps_response()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        CipherResponse result = await provider.GetRequiredService<ICiphersApi>().GetCipherAsync(
            TestApiSupport.AccountContext, cipherId, TestContext.Current.CancellationToken);

        Assert.Equal(cipherId, result.Id);
        Assert.Empty(result.CollectionIds);
        AssertRequest(handler, HttpMethod.Get, $"/ciphers/{cipherId}", "Bearer vault-access-token");
    }

    [Fact]
    public async Task Get_cipher_details_uses_details_route()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await provider.GetRequiredService<ICiphersApi>().GetCipherDetailsAsync(
            TestApiSupport.AccountContext, cipherId, TestContext.Current.CancellationToken);

        AssertRequest(handler, HttpMethod.Get, $"/ciphers/{cipherId}/details", "Bearer vault-access-token");
    }

    [Theory]
    [InlineData("soft-delete", "PUT", "/ciphers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/delete")]
    [InlineData("restore", "PUT", "/ciphers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/restore")]
    [InlineData("unarchive", "PUT", "/ciphers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/unarchive")]
    public async Task Cipher_lifecycle_uses_verified_route(string operation, string method, string path)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        ICiphersApi api = provider.GetRequiredService<ICiphersApi>();

        switch (operation)
        {
            case "soft-delete": await api.SoftDeleteCiphersAsync(TestApiSupport.AccountContext, [cipherId], TestContext.Current.CancellationToken); break;
            case "restore": await api.RestoreCiphersAsync(TestApiSupport.AccountContext, [cipherId], TestContext.Current.CancellationToken); break;
            case "unarchive": await api.UnarchiveCiphersAsync(TestApiSupport.AccountContext, [cipherId], TestContext.Current.CancellationToken); break;
        }

        AssertRequest(handler, new HttpMethod(method), path, "Bearer vault-access-token");
    }

    [Fact]
    public async Task Get_cipher_maps_encrypted_fields_dates_flags_and_attachment_metadata()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                $$"""
                {
                  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                  "organizationId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                  "folderId": "folder-id",
                  "collectionIds": ["collection-one", "collection-two"],
                  "key": "{{encoded}}",
                  "type": 3,
                  "revisionDate": "2026-01-02T03:04:05Z",
                  "creationDate": "2026-01-01T02:03:04Z",
                  "deletedDate": "2026-01-03T04:05:06Z",
                  "archivedDate": "2026-01-04T05:06:07Z",
                  "favorite": 1,
                  "reprompt": false,
                  "edit": true,
                  "viewPassword": 0,
                  "data": "{\"fields\":\"encrypted data\"}",
                  "attachments": [{
                    "id": "attachment-id",
                    "url": "https://blob.bitwarden.test/download?signature=abc",
                    "fileName": "{{encoded}}",
                    "key": "{{encoded}}",
                    "size": "42"
                  }]
                }
                """));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        CipherResponse result = await provider.GetRequiredService<ICiphersApi>().GetCipherAsync(
            TestApiSupport.AccountContext,
            cipherId,
            TestContext.Current.CancellationToken);

        Assert.Equal(cipherId, result.Id);
        Assert.Equal(OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), result.OrganizationId);
        Assert.Equal(FolderId.Parse("folder-id"), result.FolderId);
        Assert.Equal([CollectionId.Parse("collection-one"), CollectionId.Parse("collection-two")], result.CollectionIds);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), result.ProtectedCipherKey);
        Assert.Equal(CipherType.Card, result.VaultCipherType);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), result.RevisionDate);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 2, 3, 4, TimeSpan.Zero), result.CreationDate);
        Assert.Equal(new DateTimeOffset(2026, 1, 3, 4, 5, 6, TimeSpan.Zero), result.DeletedDate);
        Assert.Equal(new DateTimeOffset(2026, 1, 4, 5, 6, 7, TimeSpan.Zero), result.ArchivedDate);
        Assert.True(result.Favorite);
        Assert.False(result.Reprompt);
        Assert.True(result.Edit);
        Assert.False(result.ViewPassword);
        Assert.Equal("{\"fields\":\"encrypted data\"}"u8.ToArray(), result.Data);
        AttachmentDownloadResponse attachment = Assert.Single(result.Attachments!);
        Assert.Equal(AttachmentId.Parse("attachment-id"), attachment.Id);
        Assert.Equal("https://blob.bitwarden.test/download?signature=abc", attachment.Url);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), attachment.EncryptedFileName);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), attachment.ProtectedAttachmentKey);
        Assert.Equal(new FileSize(42), attachment.Size);
        AssertRequest(handler, HttpMethod.Get, $"/ciphers/{cipherId}", "Bearer vault-access-token");
    }

    [Fact]
    public async Task Create_cipher_posts_authenticated_json_and_maps_response()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        CipherResponse result = await provider.GetRequiredService<ICiphersApi>().CreateCipherAsync(
            TestApiSupport.AccountContext, CreateCipherRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), result.Id);
        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Post, "/ciphers", "Bearer vault-access-token");
        Assert.Contains("\"type\":1", request.Content);
        Assert.Contains("\"favorite\":true", request.Content);
    }

    [Fact]
    public async Task Update_cipher_puts_authenticated_json_to_cipher_route()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        CipherResponse result = await provider.GetRequiredService<ICiphersApi>().UpdateCipherAsync(
            TestApiSupport.AccountContext, cipherId, CreateCipherRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(cipherId, result.Id);
        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Put, $"/ciphers/{cipherId}", "Bearer vault-access-token");
        Assert.Contains("\"type\":1", request.Content);
    }

    [Fact]
    public async Task Delete_cipher_uses_authenticated_delete_route()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await provider.GetRequiredService<ICiphersApi>().DeleteCiphersAsync(
            TestApiSupport.AccountContext, [cipherId], TestContext.Current.CancellationToken);

        AssertRequest(handler, HttpMethod.Delete, $"/ciphers/{cipherId}", "Bearer vault-access-token");
    }

    [Fact]
    public async Task Delete_one_cipher_uses_single_item_route()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await provider.GetRequiredService<ICiphersApi>().DeleteCiphersAsync(
            TestApiSupport.AccountContext, [cipherId], TestContext.Current.CancellationToken);

        AssertRequest(handler, HttpMethod.Delete, $"/ciphers/{cipherId}", "Bearer vault-access-token");
    }

    [Fact]
    public async Task Create_cipher_with_collections_posts_wrapped_encrypted_cipher()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CollectionId collectionId = CollectionId.Parse("collection-one");
        await provider.GetRequiredService<ICiphersApi>().CreateCipherAsync(
            TestApiSupport.AccountContext, CreateCipherRequest(), [collectionId], TestContext.Current.CancellationToken);

        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Post, "/ciphers/create", "Bearer vault-access-token");
        Assert.Contains("\"cipher\":{", request.Content);
        Assert.Contains("\"collectionIds\":[\"collection-one\"]", request.Content);
        Assert.Contains("\"name\":\"2.", request.Content);
    }

    [Fact]
    public async Task Move_ciphers_sends_typed_ids_and_folder_id()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        FolderId folderId = FolderId.Parse("folder-id");
        await provider.GetRequiredService<ICiphersApi>().MoveCiphersAsync(TestApiSupport.AccountContext, [cipherId], folderId, TestContext.Current.CancellationToken);

        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Put, "/ciphers/move", "Bearer vault-access-token");
        Assert.Contains($"\"ids\":[\"{cipherId}\"]", request.Content);
        Assert.Contains("\"folderId\":\"folder-id\"", request.Content);
    }

    [Theory]
    [InlineData("archive-one", HttpStatusCode.OK)]
    [InlineData("archive-one", HttpStatusCode.NoContent)]
    [InlineData("archive-one", HttpStatusCode.BadRequest)]
    [InlineData("archive-many", HttpStatusCode.OK)]
    [InlineData("archive-many", HttpStatusCode.NoContent)]
    [InlineData("archive-many", HttpStatusCode.BadRequest)]
    [InlineData("unarchive-one", HttpStatusCode.OK)]
    [InlineData("unarchive-one", HttpStatusCode.NoContent)]
    [InlineData("unarchive-one", HttpStatusCode.BadRequest)]
    public async Task Archive_commands_check_status_without_requiring_a_response_body(string operation, HttpStatusCode statusCode)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => new HttpResponseMessage(statusCode));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        ICiphersApi api = provider.GetRequiredService<ICiphersApi>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Func<Task> execute = operation switch
        {
            "archive-one" => () => api.ArchiveCiphersAsync(TestApiSupport.AccountContext, [cipherId], cancellationToken),
            "archive-many" => () => api.ArchiveCiphersAsync(TestApiSupport.AccountContext, [cipherId, CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")], cancellationToken),
            "unarchive-one" => () => api.UnarchiveCiphersAsync(TestApiSupport.AccountContext, [cipherId], cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        if (statusCode == HttpStatusCode.BadRequest)
        {
            var exception = await Assert.ThrowsAsync<BitwardenApiException>(execute);
            Assert.Equal(statusCode, exception.StatusCode);
        }
        else
        {
            await execute();
        }

        string path = operation switch
        {
            "archive-one" => $"/ciphers/{cipherId}/archive",
            "archive-many" => "/ciphers/archive",
            _ => $"/ciphers/{cipherId}/unarchive"
        };
        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Put, path, "Bearer vault-access-token");
        if (operation == "archive-one")
            Assert.Null(request.Content);
        else if (operation == "archive-many")
            Assert.Contains($"\"ids\":[\"{cipherId}\",\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\"]", request.Content);
    }

    [Fact]
    public async Task Share_cipher_includes_cipher_id_and_collection_assignment()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await provider.GetRequiredService<ICiphersApi>().ShareCiphersAsync(TestApiSupport.AccountContext, [CreateCipherRequest(cipherId)], [], TestContext.Current.CancellationToken);

        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Put, $"/ciphers/{cipherId}/share", "Bearer vault-access-token");
        Assert.Contains($"\"id\":\"{cipherId}\"", request.Content);
        Assert.Contains("\"cipher\":{", request.Content);
    }

    [Fact]
    public async Task Update_cipher_partial_sends_favorite_and_folder()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(CipherResponseJson));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await provider.GetRequiredService<ICiphersApi>().UpdateCipherPartialAsync(TestApiSupport.AccountContext, cipherId, FolderId.Empty, true, TestContext.Current.CancellationToken);

        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Put, $"/ciphers/{cipherId}/partial", "Bearer vault-access-token");
        Assert.Contains("\"favorite\":true", request.Content);
        Assert.Contains("\"folderId\":null", request.Content);
    }

    [Fact]
    public async Task Share_ciphers_uses_put_and_keeps_cipher_fields_encrypted()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherRequest cipher = CreateCipherRequest(CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        await provider.GetRequiredService<ICiphersApi>().ShareCiphersAsync(TestApiSupport.AccountContext, [cipher], [], TestContext.Current.CancellationToken);

        TestApiSupport.RecordedRequest request = AssertRequest(handler, HttpMethod.Put, "/ciphers/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/share", "Bearer vault-access-token");
        Assert.Contains("\"cipher\":{", request.Content);
        Assert.Contains("\"key\":\"2.", request.Content);
        Assert.Contains("\"id\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"", request.Content);
    }

    [Theory]
    [InlineData("delete", "DELETE", "/ciphers")]
    [InlineData("soft-delete", "PUT", "/ciphers/delete")]
    [InlineData("restore", "PUT", "/ciphers/restore")]
    [InlineData("archive", "PUT", "/ciphers/archive")]
    [InlineData("unarchive", "PUT", "/ciphers/unarchive")]
    [InlineData("share", "PUT", "/ciphers/share")]
    public async Task Cipher_collection_commands_use_bulk_route_for_multiple_items(string operation, string method, string path)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId first = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        CipherId second = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        ICiphersApi api = provider.GetRequiredService<ICiphersApi>();
        switch (operation)
        {
            case "delete": await api.DeleteCiphersAsync(TestApiSupport.AccountContext, [first, second], TestContext.Current.CancellationToken); break;
            case "soft-delete": await api.SoftDeleteCiphersAsync(TestApiSupport.AccountContext, [first, second], TestContext.Current.CancellationToken); break;
            case "restore": await api.RestoreCiphersAsync(TestApiSupport.AccountContext, [first, second], TestContext.Current.CancellationToken); break;
            case "archive": await api.ArchiveCiphersAsync(TestApiSupport.AccountContext, [first, second], TestContext.Current.CancellationToken); break;
            case "unarchive": await api.UnarchiveCiphersAsync(TestApiSupport.AccountContext, [first, second], TestContext.Current.CancellationToken); break;
            case "share": await api.ShareCiphersAsync(TestApiSupport.AccountContext, [CreateCipherRequest(first), CreateCipherRequest(second)], [], TestContext.Current.CancellationToken); break;
        }

        TestApiSupport.RecordedRequest request = AssertRequest(handler, new HttpMethod(method), path, "Bearer vault-access-token");
        Assert.Contains(operation == "share" ? "\"ciphers\":[{" : "\"ids\":[", request.Content);
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("soft-delete")]
    [InlineData("restore")]
    [InlineData("archive")]
    [InlineData("unarchive")]
    [InlineData("share")]
    public async Task Cipher_collection_commands_reject_null_and_empty_without_sending(string operation)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        ICiphersApi api = provider.GetRequiredService<ICiphersApi>();

        Func<IReadOnlyList<CipherId>, IReadOnlyList<CipherRequest>, Task> execute = (ids, ciphers) => operation switch
        {
            "delete" => api.DeleteCiphersAsync(TestApiSupport.AccountContext, ids, TestContext.Current.CancellationToken),
            "soft-delete" => api.SoftDeleteCiphersAsync(TestApiSupport.AccountContext, ids, TestContext.Current.CancellationToken),
            "restore" => api.RestoreCiphersAsync(TestApiSupport.AccountContext, ids, TestContext.Current.CancellationToken),
            "archive" => api.ArchiveCiphersAsync(TestApiSupport.AccountContext, ids, TestContext.Current.CancellationToken),
            "unarchive" => api.UnarchiveCiphersAsync(TestApiSupport.AccountContext, ids, TestContext.Current.CancellationToken),
            "share" => api.ShareCiphersAsync(TestApiSupport.AccountContext, ciphers, [], TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        Func<Task> sendNull = () => execute(null!, null!);
        Func<Task> sendEmpty = () => execute([], []);
        await Assert.ThrowsAsync<ArgumentNullException>(sendNull);
        await Assert.ThrowsAsync<ArgumentException>(sendEmpty);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Sharing_one_cipher_without_an_id_is_rejected_without_sending()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetRequiredService<ICiphersApi>().ShareCiphersAsync(
            TestApiSupport.AccountContext, [CreateCipherRequest()], [], TestContext.Current.CancellationToken));

        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public async Task Cipher_operations_throw_for_failed_server_status(string operation)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{}", HttpStatusCode.NotFound));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        CipherId cipherId = CipherId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Func<Task> send = async () =>
        {
            ICiphersApi api = provider.GetRequiredService<ICiphersApi>();
            switch (operation)
            {
                case "Get":
                    await api.GetCipherAsync(TestApiSupport.AccountContext, cipherId, TestContext.Current.CancellationToken);
                    break;
                case "Create":
                    await api.CreateCipherAsync(TestApiSupport.AccountContext, CreateCipherRequest(), TestContext.Current.CancellationToken);
                    break;
                case "Update":
                    await api.UpdateCipherAsync(TestApiSupport.AccountContext, cipherId, CreateCipherRequest(), TestContext.Current.CancellationToken);
                    break;
                case "Delete":
                    await api.DeleteCiphersAsync(TestApiSupport.AccountContext, [cipherId], TestContext.Current.CancellationToken);
                    break;
            }
        };

        await Assert.ThrowsAsync<BitwardenApiException>(send);
        Assert.Single(handler.Requests);
    }

    private const string CipherResponseJson = """
        {"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","type":1,"revisionDate":"2026-01-02T00:00:00Z","creationDate":"2026-01-01T00:00:00Z","favorite":false,"reprompt":false,"edit":true,"viewPassword":true,"data":"{}"}
        """;

    private static CipherRequest CreateCipherRequest(CipherId id = default) => new()
    {
        Id = id,
        Type = CipherType.Login,
        Favorite = true,
        Reprompt = 0,
        Key = TestApiSupport.ParseEncString("2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y="),
        Name = TestApiSupport.ParseEncString("2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=")
    };

    private static TestApiSupport.RecordedRequest AssertRequest(
        TestApiSupport.SnapshottingHttpMessageHandler handler,
        HttpMethod method,
        string path,
        string authorization)
    {
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(method, request.Method);
        Assert.Equal(path, request.RequestUri.AbsolutePath);
        Assert.Equal([authorization], request.Headers["Authorization"]);
        return request;
    }
}
