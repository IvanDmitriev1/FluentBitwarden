using System.Net.Http.Json;

using BitwardenApi.Client;
using BitwardenApi.Vault.Attachments.Responses;
using BitwardenApi.Client.Http;
using BitwardenApi.Vault.Internal;

namespace BitwardenApi.Vault.Attachments;

internal sealed class AttachmentsApi(IHttpClientFactory httpClientFactory) : IAttachmentsApi
{
    public async Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        AttachmentId attachmentId,
        AttachmentStreamHandler streamHandler,
        CancellationToken cancellationToken = default)
    {
        cipherId.ThrowIfEmpty();
        attachmentId.ThrowIfEmpty();

        var downloadResponse = await GetAttachmentDownloadResponseAsync(
            accountContext,
            cipherId,
            attachmentId,
            cancellationToken);

        if (!Uri.TryCreate(downloadResponse.Url, UriKind.Absolute, out var downloadUri))
        {
            throw new InvalidOperationException(
                $"Attachment download URL is not an absolute URI for attachment '{attachmentId}'.");
        }

        using var downloadClient = httpClientFactory.CreateClient(HttpClientNames.AttachmentDownload);
        using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        using var downloadHttpResponse = await downloadClient.SendAsync(
            downloadRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        downloadHttpResponse.EnsureSuccess("Vault attachment download", cancellationToken);

        await using var stream = await downloadHttpResponse.Content.ReadAsStreamAsync(cancellationToken);
        await streamHandler.Invoke(stream, downloadResponse.ProtectedAttachmentKey);
    }

    private async Task<AttachmentDownloadResponse> GetAttachmentDownloadResponseAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        AttachmentId attachmentId,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Vault);

        Uri requestUri = new(
            accountContext.Environment.ApiBase,
            $"/ciphers/{cipherId}/attachment/{attachmentId}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault attachment metadata", cancellationToken);

        var downloadResponse = await response.Content.ReadFromJsonAsync(
            VaultJsonContext.ConfiguredDefault.AttachmentDownloadResponse,
            cancellationToken);

        return downloadResponse;
    }
}
