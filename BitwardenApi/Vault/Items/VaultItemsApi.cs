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

    public async Task<VaultCipherDto> GetCipherAsync(
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
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return JsonSerializer.Deserialize(stream, VaultJsonContext.Default.VaultCipherDto);
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

