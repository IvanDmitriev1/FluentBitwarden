namespace FluentBitwarden.Contracts.Shared;

public static class HttpClientFactoryExtensions
{
    public static HttpClient CreateSharedClient(this IHttpClientFactory factory) =>
        factory.CreateClient("SharedHttpClient");
}