using System.Net.Http.Json;

using BitwardenApi.Client;
using BitwardenApi.Vault.Sync.Responses;
using BitwardenApi.Client.Http;
using BitwardenApi.Vault.Internal;

namespace BitwardenApi.Vault.Sync;

internal sealed class VaultSyncApi(IHttpClientFactory httpClientFactory) : IVaultSyncApi
{
    public async Task<DateTimeOffset> GetRevisionDateAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);

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
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);

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

}
