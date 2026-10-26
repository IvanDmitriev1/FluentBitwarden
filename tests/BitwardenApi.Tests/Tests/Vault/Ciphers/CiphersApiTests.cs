using BitwardenApi.Tests.Infrastructure;
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

        await provider.GetRequiredService<ICiphersApi>().DeleteCipherAsync(
            TestApiSupport.AccountContext, cipherId, TestContext.Current.CancellationToken);

        AssertRequest(handler, HttpMethod.Delete, $"/ciphers/{cipherId}", "Bearer vault-access-token");
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
                    await api.DeleteCipherAsync(TestApiSupport.AccountContext, cipherId, TestContext.Current.CancellationToken);
                    break;
            }
        };

        await Assert.ThrowsAsync<BitwardenApiException>(send);
        Assert.Single(handler.Requests);
    }

    private const string CipherResponseJson = """
        {"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","type":1,"revisionDate":"2026-01-02T00:00:00Z","creationDate":"2026-01-01T00:00:00Z","favorite":false,"reprompt":false,"edit":true,"viewPassword":true,"data":"{}"}
        """;

    private static CipherRequest CreateCipherRequest() => new()
    {
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
