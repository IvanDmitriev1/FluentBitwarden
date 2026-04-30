using System.Net.Http;

namespace FluentBitwarden.Shared.Extensions;

internal static class HttpClientFactoryExtensions
{
    public static HttpClient CreateSharedClient(this IHttpClientFactory factory) =>
        factory.CreateClient("SharedHttpClient");
}