using System.Net.Http.Json;

namespace BitwardenApi.Vault.Items;

internal sealed class VaultItemsApi(
    IHttpClientFactory httpClientFactory,
    IBitwardenEnvironmentAccessor environmentAccessor) : IVaultItemsApi
{
    public async Task<DateTimeOffset> GetRevisionDateAsync(
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        BitwardenEnvironment environment = environmentAccessor.CurrentEnvironment;
        Uri requestUri = new(environment.ApiBase, "/accounts/revision-date");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);

        response.EnsureSuccess("Vault get revision date", cancellationToken);

        var rawValue = await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.Int64, cancellationToken);
        if (rawValue < 0)
            throw new BitwardenAuthorizationException();

        var revision = DateTimeOffset.FromUnixTimeMilliseconds(rawValue);
        return revision;
    }

    public async Task<VaultSyncResponse> GetSyncAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        BitwardenEnvironment environment = environmentAccessor.CurrentEnvironment;
        Uri requestUri = new(environment.ApiBase, "/sync?excludeDomains=true");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault sync", cancellationToken);

        return await response.Content.ReadFromJsonAsync<VaultSyncResponse>(
            VaultJsonContext.ConfiguredDefault.VaultSyncResponse,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
    }

    public async Task<VaultCipherDto> GetCipherAsync(CipherId cipherId, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        BitwardenEnvironment environment = environmentAccessor.CurrentEnvironment;
        Uri requestUri = new(environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault get vaultCipher", cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return JsonSerializer.Deserialize(stream, VaultJsonContext.Default.VaultCipherDto);
    }

    public async Task DeleteCipherAsync(
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        BitwardenEnvironment environment = environmentAccessor.CurrentEnvironment;
        Uri requestUri = new(environment.ApiBase, $"/ciphers/{cipherId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);

        response.EnsureSuccess("Vault delete vaultCipher", cancellationToken);
    }
}

