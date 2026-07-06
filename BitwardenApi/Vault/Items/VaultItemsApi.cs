using System.Net.Http.Json;

namespace BitwardenApi.Vault.Items;

internal sealed class VaultItemsApi(
    IHttpClientFactory httpClientFactory) : IVaultItemsApi
{
    public async Task<DateTimeOffset> GetRevisionDateAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(accountContext.Environment.ApiBase, "/accounts/revision-date");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccess("Vault get revision date", cancellationToken);

        var rawValue = await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.Int64, cancellationToken);
        if (rawValue < 0)
            throw new BitwardenAuthorizationException();

        var revision = DateTimeOffset.FromUnixTimeMilliseconds(rawValue);
        return revision;
    }

    public async Task<VaultSyncResponse> GetSyncAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(accountContext.Environment.ApiBase, "/sync?excludeDomains=true");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault sync", cancellationToken);

        return await response.Content.ReadFromJsonAsync<VaultSyncResponse>(
            VaultJsonContext.ConfiguredDefault.VaultSyncResponse,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
    }

    public async Task<VaultCipherResponse> GetCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(accountContext.Environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault get vaultCipher", cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.Default.VaultCipherResponse, cancellationToken);
    }

    public Task<VaultCipherResponse> CreateCipherAsync(
        BitwardenAccountContext accountContext,
        VaultCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(accountContext.Environment.ApiBase, "/ciphers");
        return SendCipherAsync(accountContext, HttpMethod.Post, requestUri, request, "Vault create vaultCipher", cancellationToken);
    }

    public Task<VaultCipherResponse> UpdateCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        VaultCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(accountContext.Environment.ApiBase, $"/ciphers/{cipherId:D}");
        return SendCipherAsync(accountContext, HttpMethod.Put, requestUri, request, "Vault update vaultCipher", cancellationToken);
    }

    private async Task<VaultCipherResponse> SendCipherAsync(
        BitwardenAccountContext accountContext,
        HttpMethod method,
        Uri requestUri,
        VaultCipherRequest request,
        string operationName,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        using var requestMessage = new HttpRequestMessage(method, requestUri);
        requestMessage.Content = JsonContent.Create(request, VaultJsonContext.Default.VaultCipherRequest);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess(operationName, cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.Default.VaultCipherResponse, cancellationToken);
    }

    public async Task DeleteCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(accountContext.Environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccess("Vault delete vaultCipher", cancellationToken);
    }
}

