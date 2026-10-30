using System.Net.Http.Json;
using BitwardenApi.Client;
using BitwardenApi.Client.Http;
using BitwardenApi.Vault.Folders.Responses;
using BitwardenApi.Vault.Folders.Requests;
using BitwardenApi.Vault.Internal;

namespace BitwardenApi.Vault.Folders;

internal sealed class FoldersApi(IHttpClientFactory httpClientFactory) : IFoldersApi
{
    public async Task<IReadOnlyList<FolderResponse>> GetFoldersAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(accountContext.Environment.ApiBase, "/folders"));
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccess("Vault get folders", cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The folder list response must contain a data array.");
        }

        return data.Deserialize(VaultJsonContext.ConfiguredDefault.ListFolderResponse) ??
               throw new JsonException("Failed to deserialize folders.");
    }

    public async Task<FolderResponse> GetFolderAsync(
        BitwardenAccountContext accountContext,
        FolderId folderId,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        Uri requestUri = new(accountContext.Environment.ApiBase, $"/folders/{folderId.Value:D}");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault get folder", cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.FolderResponse, cancellationToken);
    }

    public Task<FolderResponse> CreateFolderAsync(BitwardenAccountContext accountContext, FolderRequest request, CancellationToken cancellationToken = default) =>
        SendFolderAsync(accountContext, HttpMethod.Post, new Uri(accountContext.Environment.ApiBase, "/folders"), request, "Vault create folder", cancellationToken);

    public Task<FolderResponse> UpdateFolderAsync(BitwardenAccountContext accountContext, FolderId folderId, FolderRequest request, CancellationToken cancellationToken = default) =>
        SendFolderAsync(accountContext, HttpMethod.Put, new Uri(accountContext.Environment.ApiBase, $"/folders/{folderId.Value:D}"), request, "Vault update folder", cancellationToken);

    public Task DeleteFoldersAsync(BitwardenAccountContext accountContext, IReadOnlyList<FolderId> folderIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folderIds);
        if (folderIds.Count == 0)
            throw new ArgumentException("At least one folder ID is required.", nameof(folderIds));

        return folderIds.Count == 1
            ? SendDeleteAsync(accountContext, $"/folders/{folderIds[0].Value:D}", null, "Vault delete folder", cancellationToken)
            : SendDeleteAsync(accountContext, "/folders",
                JsonContent.Create(new FolderBulkDeleteRequest { Ids = folderIds }, VaultJsonContext.ConfiguredDefault.FolderBulkDeleteRequest),
                "Vault delete folders", cancellationToken);
    }

    public async Task DeleteAllFoldersAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default) =>
        await SendDeleteAsync(accountContext, "/folders/all", null, "Vault delete all folders", cancellationToken);

    private async Task<FolderResponse> SendFolderAsync(BitwardenAccountContext accountContext, HttpMethod method, Uri requestUri, FolderRequest request, string operation, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(method, requestUri);
        requestMessage.Content = JsonContent.Create(request, VaultJsonContext.ConfiguredDefault.FolderRequest);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccess(operation, cancellationToken);
        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.FolderResponse, cancellationToken);
    }

    private async Task SendDeleteAsync(BitwardenAccountContext accountContext, string path, HttpContent? content, string operation, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, new Uri(accountContext.Environment.ApiBase, path));
        requestMessage.Content = content;
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccess(operation, cancellationToken);
    }
}
