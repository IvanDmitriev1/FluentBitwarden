namespace BitwardenApi.Tests.Infrastructure;

internal static class TestApiSupport
{
    public static readonly BitwardenClientContext ClientContext = new(
        new BitwardenEnvironment(
            new Uri("https://api.bitwarden.test"),
            new Uri("https://identity.bitwarden.test"),
            new Uri("https://notifications.bitwarden.test"),
            new Uri("https://vault.bitwarden.test")),
        new DeviceInfo(DeviceIdentifier.Parse("device-id"), DeviceName.Parse("device-name")));

    public static readonly BitwardenAccountContext AccountContext = new(
        UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        ClientContext.Environment);

    public static ServiceProvider CreateProvider(
        SnapshottingHttpMessageHandler? identityHandler = null,
        SnapshottingHttpMessageHandler? vaultHandler = null,
        SnapshottingHttpMessageHandler? attachmentHandler = null)
    {
        var services = new ServiceCollection();
        services.AddBitwardenApi();
        services.AddSingleton<IBitwardenAccessTokenProvider>(new FixedAccessTokenProvider());

        if (identityHandler is not null)
        {
            services.AddHttpClient("BitwardenApiIdentityHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() => identityHandler);
        }

        if (vaultHandler is not null)
        {
            services.AddHttpClient("BitwardenApiVaultHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() => vaultHandler);
        }

        if (attachmentHandler is not null)
        {
            services.AddHttpClient("BitwardenApiAttachmentDownloadHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() => attachmentHandler);
        }

        return services.BuildServiceProvider();
    }

    public static HttpResponseMessage JsonResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage BytesResponse(byte[] content) =>
        new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
        };

    public static IReadOnlyDictionary<string, string> ParseForm(string? content)
    {
        Assert.NotNull(content);

        return content.Split('&')
            .Select(static pair => pair.Split('=', 2))
            .ToDictionary(
                static pair => Uri.UnescapeDataString(pair[0]),
                static pair => Uri.UnescapeDataString(pair[1].Replace('+', ' ')),
                StringComparer.Ordinal);
    }

    public static async Task<SessionTokenRejection> GetPasswordLoginRejectionAsync(string payload)
    {
        using var handler = new SnapshottingHttpMessageHandler(() => JsonResponse(payload, HttpStatusCode.BadRequest));
        using ServiceProvider provider = CreateProvider(identityHandler: handler);

        SessionTokenResult<TokenAuthenticatedModel> result = await provider
            .GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(ClientContext, "user@example.test", "password-hash"),
                TestContext.Current.CancellationToken);

        return Assert.IsType<SessionTokenResult<TokenAuthenticatedModel>.Rejected>(result).Error;
    }

    public static EncString ParseEncString(string encoded)
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes($"\"{encoded}\""));
        Assert.True(reader.Read());
        return EncString.CreateFrom(ref reader);
    }

    private static async Task<RecordedRequest> CaptureRequestAsync(HttpRequestMessage request)
    {
        string? content = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return new RecordedRequest(
            request.Method,
            request.RequestUri!,
            content,
            request.Headers.ToDictionary(
                static header => header.Key,
                static header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase));
    }

    private sealed class FixedAccessTokenProvider : IBitwardenAccessTokenProvider
    {
        public ValueTask<AccessToken> GetAccessTokenAsync(
            BitwardenAccountContext accountContext,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(AccessToken.Parse("vault-access-token"));
    }

    internal sealed class SnapshottingHttpMessageHandler(
        Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(await CaptureRequestAsync(request));
            HttpResponseMessage response = responseFactory();
            response.RequestMessage = request;
            return response;
        }
    }

    internal sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string? Content,
        IReadOnlyDictionary<string, string[]> Headers);
}
