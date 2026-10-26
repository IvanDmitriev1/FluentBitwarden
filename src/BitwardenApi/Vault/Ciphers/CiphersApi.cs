using System.Net.Http.Json;

using BitwardenApi.Client;
using BitwardenApi.Vault.Ciphers.Requests;
using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Client.Http;
using BitwardenApi.Vault.Internal;

namespace BitwardenApi.Vault.Ciphers;

internal sealed class CiphersApi(IHttpClientFactory httpClientFactory) : ICiphersApi
{
    public async Task<CipherResponse> GetCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);

        Uri requestUri = new(accountContext.Environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault get vaultCipher", cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.CipherResponse, cancellationToken);
    }

    public Task<CipherResponse> CreateCipherAsync(
        BitwardenAccountContext accountContext,
        CipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(accountContext.Environment.ApiBase, "/ciphers");
        return SendCipherAsync(accountContext, HttpMethod.Post, requestUri, request, "Vault create vaultCipher", cancellationToken);
    }

    public Task<CipherResponse> UpdateCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(accountContext.Environment.ApiBase, $"/ciphers/{cipherId:D}");
        return SendCipherAsync(accountContext, HttpMethod.Put, requestUri, request, "Vault update vaultCipher", cancellationToken);
    }

    private async Task<CipherResponse> SendCipherAsync(
        BitwardenAccountContext accountContext,
        HttpMethod method,
        Uri requestUri,
        CipherRequest request,
        string operationName,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);

        using var requestMessage = new HttpRequestMessage(method, requestUri);
        requestMessage.Content = JsonContent.Create(request, VaultJsonContext.Default.CipherRequest);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess(operationName, cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.CipherResponse, cancellationToken);
    }

    public async Task DeleteCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);

        Uri requestUri = new(accountContext.Environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccess("Vault delete vaultCipher", cancellationToken);
    }
}
