namespace BitwardenApi.Infrastructure.Transport;

internal static class HttpClientFactoryExtensions
{
    public static HttpClient CreateIdentityClient(this IHttpClientFactory factory) =>
        factory.CreateClient("BitwardenApiIdentityHttpClient");

    public static HttpClient CreateVaultClient(this IHttpClientFactory factory) =>
        factory.CreateClient("BitwardenApiVaultHttpClient");

    public static HttpClient CreateAttachmentDownloadClient(this IHttpClientFactory factory) =>
        factory.CreateClient("BitwardenApiAttachmentDownloadHttpClient");
}

