using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Vault.Folders;
using BitwardenApi.Vault.Folders.Requests;

namespace BitwardenApi.Tests.Tests;

public sealed class FoldersApiTests
{
    private const string EncodedName = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";

    [Fact]
    public async Task Get_folders_reads_data_envelope_and_empty_list()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse($$"""{"data":[{"id":"folder-id","name":"{{EncodedName}}","revisionDate":"2026-01-02T03:04:05Z"}]}"""));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        IReadOnlyList<FolderResponse> folders = await provider.GetRequiredService<IFoldersApi>().GetFoldersAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken);
        FolderResponse folder = Assert.Single(folders);
        Assert.Equal(FolderId.Parse("folder-id"), folder.Id);
        Assert.Equal(TestApiSupport.ParseEncString(EncodedName), folder.EncryptedName);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), folder.RevisionDate);
        Assert.Equal("/folders", Assert.Single(handler.Requests).RequestUri.AbsolutePath);

        handler.Requests.Clear();
        // A separate response is needed because the handler response factory is fixed.
        using var emptyHandler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{\"data\":[]}"));
        using ServiceProvider emptyProvider = TestApiSupport.CreateProvider(vaultHandler: emptyHandler);
        Assert.Empty(await emptyProvider.GetRequiredService<IFoldersApi>().GetFoldersAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"data\":null}")]
    public async Task Get_folders_rejects_malformed_list_response(string json)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(json));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        await Assert.ThrowsAsync<JsonException>(() => provider.GetRequiredService<IFoldersApi>()
            .GetFoldersAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Create", "POST", "/folders")]
    [InlineData("Update", "PUT", "/folders/folder-id")]
    public async Task Create_and_update_send_encrypted_name(string operation, string method, string path)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse($$"""{"id":"folder-id","name":"{{EncodedName}}","revisionDate":"2026-01-02T03:04:05Z"}"""));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        IFoldersApi api = provider.GetRequiredService<IFoldersApi>();
        FolderResponse response = operation == "Create"
            ? await api.CreateFolderAsync(TestApiSupport.AccountContext, new FolderRequest { Name = TestApiSupport.ParseEncString(EncodedName) }, TestContext.Current.CancellationToken)
            : await api.UpdateFolderAsync(TestApiSupport.AccountContext, FolderId.Parse("folder-id"), new FolderRequest { Name = TestApiSupport.ParseEncString(EncodedName) }, TestContext.Current.CancellationToken);
        Assert.Equal(FolderId.Parse("folder-id"), response.Id);
        Assert.Equal(TestApiSupport.ParseEncString(EncodedName), response.EncryptedName);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), response.RevisionDate);
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(new HttpMethod(method), request.Method);
        Assert.Equal(path, request.RequestUri.AbsolutePath);
        using JsonDocument body = JsonDocument.Parse(Assert.IsType<string>(request.Content));
        Assert.Equal(EncodedName, body.RootElement.GetProperty("name").GetString());
        Assert.Equal(["Bearer vault-access-token"], request.Headers["Authorization"]);
    }

    [Theory]
    [InlineData("List")]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Single")]
    [InlineData("Bulk")]
    [InlineData("All")]
    public async Task Folder_operations_propagate_server_errors(string operation)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("{}", HttpStatusCode.BadRequest));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        IFoldersApi api = provider.GetRequiredService<IFoldersApi>();
        FolderId folderId = FolderId.Parse("folder-id");

        Func<Task> send = operation switch
        {
            "List" => () => api.GetFoldersAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken),
            "Create" => () => api.CreateFolderAsync(TestApiSupport.AccountContext, new FolderRequest { Name = TestApiSupport.ParseEncString(EncodedName) }, TestContext.Current.CancellationToken),
            "Update" => () => api.UpdateFolderAsync(TestApiSupport.AccountContext, folderId, new FolderRequest { Name = TestApiSupport.ParseEncString(EncodedName) }, TestContext.Current.CancellationToken),
            "Single" => () => api.DeleteFoldersAsync(TestApiSupport.AccountContext, [folderId], TestContext.Current.CancellationToken),
            "Bulk" => () => api.DeleteFoldersAsync(TestApiSupport.AccountContext, [folderId, FolderId.Parse("other-folder")], TestContext.Current.CancellationToken),
            _ => () => api.DeleteAllFoldersAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken)
        };

        BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(send);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("Single", "/folders/folder-id", "")]
    [InlineData("Bulk", "/folders", "{\"ids\":[\"folder-id\",\"other-folder\"]}")]
    [InlineData("All", "/folders/all", "")]
    public async Task Delete_operations_use_supported_routes(string operation, string path, string body)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NoContent));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        IFoldersApi api = provider.GetRequiredService<IFoldersApi>();
        FolderId folderId = FolderId.Parse("folder-id");
        if (operation == "Single") await api.DeleteFoldersAsync(TestApiSupport.AccountContext, [folderId], TestContext.Current.CancellationToken);
        else if (operation == "Bulk") await api.DeleteFoldersAsync(TestApiSupport.AccountContext, [folderId, FolderId.Parse("other-folder")], TestContext.Current.CancellationToken);
        else await api.DeleteAllFoldersAsync(TestApiSupport.AccountContext, TestContext.Current.CancellationToken);
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal(path, request.RequestUri.AbsolutePath);
        Assert.Equal(body.Length == 0 ? null : body, request.Content);
        Assert.Equal(["Bearer vault-access-token"], request.Headers["Authorization"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_folders_rejects_null_or_empty_collection_without_sending(bool nullCollection)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NoContent));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        IFoldersApi api = provider.GetRequiredService<IFoldersApi>();
        IReadOnlyList<FolderId> folderIds = nullCollection ? null! : [];
        Func<Task> send = () => api.DeleteFoldersAsync(TestApiSupport.AccountContext, folderIds, TestContext.Current.CancellationToken);

        if (nullCollection)
            await Assert.ThrowsAsync<ArgumentNullException>(send);
        else
            await Assert.ThrowsAsync<ArgumentException>(send);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Get_folder_uses_authenticated_route_and_maps_encrypted_response()
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse($$"""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"{{encoded}}","revisionDate":"2026-01-02T03:04:05Z"}"""));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        FolderId folderId = FolderId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        FolderResponse result = await provider.GetRequiredService<IFoldersApi>().GetFolderAsync(
            TestApiSupport.AccountContext, folderId, TestContext.Current.CancellationToken);

        Assert.Equal(folderId, result.Id);
        Assert.Equal(TestApiSupport.ParseEncString(encoded), result.EncryptedName);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), result.RevisionDate);
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/folders/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", request.RequestUri.AbsolutePath);
        Assert.Equal(["Bearer vault-access-token"], request.Headers["Authorization"]);
    }

    [Theory]
    [InlineData("https://api.bitwarden.test/prefix/")]
    [InlineData("https://api.bitwarden.test/prefix")]
    public async Task Get_folder_uses_root_relative_route_for_api_base_with_path(string apiBase)
    {
        const string encoded = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse($$"""{"id":"folder-id","name":"{{encoded}}","revisionDate":"2026-01-02T03:04:05Z"}"""));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);
        BitwardenAccountContext account = TestApiSupport.AccountContext with
        {
            Environment = TestApiSupport.AccountContext.Environment with { ApiBase = new Uri(apiBase) }
        };

        await provider.GetRequiredService<IFoldersApi>().GetFolderAsync(
            account, FolderId.Parse("folder-id"), TestContext.Current.CancellationToken);

        Assert.Equal("/folders/folder-id", Assert.Single(handler.Requests).RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task Get_folder_throws_for_not_found()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("not found", HttpStatusCode.NotFound));
        using ServiceProvider provider = TestApiSupport.CreateProvider(vaultHandler: handler);

        BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(() => provider
            .GetRequiredService<IFoldersApi>()
            .GetFolderAsync(TestApiSupport.AccountContext, FolderId.Parse("folder-id"), TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Single(handler.Requests);
    }
}
