using System.Net.Http.Headers;
using BitwardenApi.Internal;

namespace BitwardenApi.Vault;

public sealed class VaultApiClient(HttpClient httpClient) : IVaultApiClient
{
    public async Task<ApiStreamResponse> GetSyncAsync(
        GetSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        string relativePath = request.ExcludeDomains
            ? "/sync?excludeDomains=true"
            : "/sync";
        Uri requestUri = new(request.Context.Environment.ApiBase, relativePath);

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);
        var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Vault sync", cancellationToken);
    }

    public async Task<ApiStreamResponse> GetCipherAsync(
        GetCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(request.Context.Environment.ApiBase, $"/ciphers/{request.CipherId.Value:D}");
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);

        HttpResponseMessage response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Vault get cipher", cancellationToken);
    }

    public async Task<ApiStreamResponse> GetAllCiphersAsync(
        GetAllCiphersRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(request.Context.Environment.ApiBase, "/ciphers");
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);
        var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Vault get all ciphers", cancellationToken);
    }

    public async Task CreateCipherAsync(
        CreateCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(request.Context.Environment.ApiBase, "/ciphers");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);
        StreamContent requestContent = new(request.Content);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestMessage.Content = requestContent;

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault create cipher", cancellationToken);
    }

    public async Task UpdateCipherAsync(
        UpdateCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(request.Context.Environment.ApiBase, $"/ciphers/{request.CipherId.Value:D}");
        using HttpRequestMessage requestMessage = new(HttpMethod.Put, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);
        StreamContent requestContent = new(request.Content);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestMessage.Content = requestContent;

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault update cipher", cancellationToken);
    }

    public async Task DeleteCipherAsync(
        DeleteCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(request.Context.Environment.ApiBase, $"/ciphers/{request.CipherId.Value:D}");

        using HttpRequestMessage requestMessage = new(HttpMethod.Delete, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault delete cipher", cancellationToken);
    }
}
