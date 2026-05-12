using BitwardenApi.Contracts;
using BitwardenApi.Models;
using BitwardenApi.Exceptions;
using BitwardenApi.Internal.Extensions;
using BitwardenApi.Internal.Serialization;
using BitwardenApi.Internal.Transport;
using System.Net.Http.Json;

namespace BitwardenApi.Clients;

internal sealed class VaultApiClient(
    IHttpClientFactory httpClientFactory,
    IBitwardenEnvironmentAccessor environmentAccessor) : IVaultApiClient
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

        var rawValue = await response.Content.ReadFromJsonAsync(BitwardenApiJsonContext.ConfiguredDefault.Int64, cancellationToken);
        if (rawValue < 0)
            throw new BitwardenAuthorizationException();

        var revision = DateTimeOffset.FromUnixTimeMilliseconds(rawValue);
        return revision;
    }

    public async Task GetSyncAsync(
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default)
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
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await streamHandler.Invoke(stream);
    }

    public async Task GetCipherAsync(
        CipherId cipherId,
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default)
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
        await streamHandler.Invoke(stream);
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
