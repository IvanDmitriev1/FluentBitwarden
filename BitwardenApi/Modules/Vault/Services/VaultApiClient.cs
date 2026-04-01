using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Shared.Context;
using BitwardenApi.Shared.Transport;

namespace BitwardenApi.Modules.Vault.Services;

public sealed class VaultApiClient(HttpClient httpClient) : IVaultApiClient
{
    public async Task<ApiStreamResponse> GetSyncAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(environment.ApiBase, "/sync");

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Vault sync", cancellationToken);
    }

    public async Task<ApiStreamResponse> GetCipherAsync(
        BitwardenEnvironment environment,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        HttpResponseMessage response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Vault get cipher", cancellationToken);
    }

    public async Task<ApiStreamResponse> GetAllCiphersAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(environment.ApiBase, "/ciphers");
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Vault get all ciphers", cancellationToken);
    }

    public async Task DeleteCipherAsync(
        BitwardenEnvironment environment,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(environment.ApiBase, $"/ciphers/{cipherId.Value:D}");

        using HttpRequestMessage requestMessage = new(HttpMethod.Delete, requestUri);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault delete cipher", cancellationToken);
    }
}
