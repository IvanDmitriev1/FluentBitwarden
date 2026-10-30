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
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(accountContext.Environment.ApiBase, $"/ciphers/{cipherId.Value:D}"));
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault get vaultCipher", cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.CipherResponse, cancellationToken);
    }

    public async Task<CipherResponse> GetCipherDetailsAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(accountContext.Environment.ApiBase, $"/ciphers/{cipherId.Value:D}/details"));
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccess("Vault get cipher details", cancellationToken);

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

    public async Task<CipherResponse> CreateCipherAsync(BitwardenAccountContext accountContext, CipherRequest request, IReadOnlyList<CollectionId> collectionIds, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(accountContext.Environment.ApiBase, "/ciphers/create"));
        requestMessage.Content =
            JsonContent.Create(new CipherCreateRequest { Cipher = request, CollectionIds = collectionIds },
                VaultJsonContext.ConfiguredDefault.CipherCreateRequest);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccess("Vault create cipher with collections", cancellationToken);

        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.CipherResponse, cancellationToken);
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

    public Task<CipherResponse> UpdateCipherPartialAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        FolderId folderId,
        bool favorite,
        CancellationToken cancellationToken = default) =>
        SendCipherBodyStatusAsync(accountContext, $"/ciphers/{cipherId.Value:D}/partial",
            JsonContent.Create(
                new CipherPartialRequest { FolderId = folderId.IsEmpty ? null : folderId.Value, Favorite = favorite },
                VaultJsonContext.ConfiguredDefault.CipherPartialRequest),
            "Vault update cipher", cancellationToken);

    public Task DeleteCiphersAsync(
        BitwardenAccountContext accountContext,
        IReadOnlyList<CipherId> cipherIds,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(cipherIds);
        return cipherIds.Count == 1
            ? SendStatusAsync(accountContext, HttpMethod.Delete, $"/ciphers/{cipherIds[0].Value:D}", null,
                "Vault delete cipher", cancellationToken)
            : SendStatusAsync(accountContext, HttpMethod.Delete, "/ciphers",
                JsonContent.Create(new CipherBulkDeleteRequest { Ids = cipherIds },
                    VaultJsonContext.ConfiguredDefault.CipherBulkDeleteRequest), "Vault delete ciphers",
                cancellationToken);
    }

    public Task SoftDeleteCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds,
        CancellationToken cancellationToken = default) => SendIdCommandAsync(accountContext, cipherIds, "/delete", "/ciphers/delete", "Vault soft delete ciphers", cancellationToken);

    public Task RestoreCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds,
        CancellationToken cancellationToken = default) => SendIdCommandAsync(accountContext, cipherIds, "/restore", "/ciphers/restore", "Vault restore ciphers", cancellationToken);

    public Task UnarchiveCiphersAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> cipherIds,
        CancellationToken cancellationToken = default) => SendIdCommandAsync(accountContext, cipherIds, "/unarchive", "/ciphers/unarchive", "Vault unarchive ciphers", cancellationToken);

    public Task ArchiveCiphersAsync(BitwardenAccountContext accountContext,
        IReadOnlyList<CipherId> cipherIds,
        CancellationToken cancellationToken = default) => SendIdCommandAsync(accountContext, cipherIds, "/archive", "/ciphers/archive", "Vault archive ciphers", cancellationToken);

    public Task MoveCiphersAsync(
        BitwardenAccountContext accountContext,
        IReadOnlyList<CipherId> cipherIds,
        FolderId folderId,
        CancellationToken cancellationToken = default) =>
        SendStatusAsync(accountContext,
            HttpMethod.Put,
            "/ciphers/move",
            JsonContent.Create(new CipherBulkMoveRequest { Ids = cipherIds, FolderId = folderId.IsEmpty ? null : folderId.Value },
                VaultJsonContext.ConfiguredDefault.CipherBulkMoveRequest),
            "Vault move ciphers", cancellationToken);

    public Task ShareCiphersAsync(
        BitwardenAccountContext accountContext,
        IReadOnlyList<CipherRequest> ciphers,
        IReadOnlyList<CollectionId> collectionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ciphers);
        if (ciphers.Count == 0) throw new ArgumentException("At least one cipher is required.", nameof(ciphers));
        if (ciphers.Count == 1)
        {
            CipherRequest cipher = ciphers[0];
            if (cipher.Id.IsEmpty) throw new ArgumentException("A cipher ID is required to share a cipher.", nameof(ciphers));
            return SendShareSingleAsync(accountContext, cipher, collectionIds, cancellationToken);
        }
        return SendStatusAsync(accountContext, HttpMethod.Put, "/ciphers/share",
            JsonContent.Create(new CipherBulkShareRequest { Ciphers = ciphers, CollectionIds = collectionIds }, VaultJsonContext.ConfiguredDefault.CipherBulkShareRequest), "Vault share ciphers", cancellationToken);
    }

    private Task SendIdCommandAsync(BitwardenAccountContext accountContext, IReadOnlyList<CipherId> ids, string suffix, string bulkPath, string operation, CancellationToken cancellationToken)
    {
        ValidateIds(ids);
        return ids.Count == 1
            ? SendStatusAsync(accountContext, HttpMethod.Put, $"/ciphers/{ids[0].Value:D}{suffix}", null, operation, cancellationToken)
            : SendStatusAsync(accountContext, HttpMethod.Put, bulkPath,
                JsonContent.Create(new CipherBulkDeleteRequest { Ids = ids }, VaultJsonContext.ConfiguredDefault.CipherBulkDeleteRequest), operation, cancellationToken);
    }

    private static void ValidateIds(IReadOnlyList<CipherId> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) throw new ArgumentException("At least one cipher ID is required.", nameof(ids));
    }

    private async Task SendShareSingleAsync(BitwardenAccountContext accountContext, CipherRequest cipher, IReadOnlyList<CollectionId> collectionIds, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(accountContext.Environment.ApiBase, $"/ciphers/{cipher.Id.Value:D}/share"));
        request.Content = JsonContent.Create(new CipherShareRequest { Cipher = cipher, CollectionIds = collectionIds }, VaultJsonContext.ConfiguredDefault.CipherShareRequest);
        request.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccess("Vault share cipher", cancellationToken);
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
        requestMessage.Content = JsonContent.Create(request, VaultJsonContext.ConfiguredDefault.CipherRequest);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess(operationName, cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.CipherResponse, cancellationToken);
    }

    private async Task SendStatusAsync(BitwardenAccountContext accountContext, HttpMethod method, string path, HttpContent? content, string operation, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(method, new Uri(accountContext.Environment.ApiBase, path));
        requestMessage.Content = content;
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccess(operation, cancellationToken);
    }

    private async Task<CipherResponse> SendCipherBodyStatusAsync(BitwardenAccountContext accountContext, string path, HttpContent content, string operation, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Put, new Uri(accountContext.Environment.ApiBase, path));
        requestMessage.Content = content;
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccess(operation, cancellationToken);

        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.CipherResponse, cancellationToken);
    }

}
