using System.Net.Http.Json;
using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault.Attachments;

internal sealed class VaultCipherAttachmentApi(IHttpClientFactory httpClientFactory) : IVaultCipherAttachmentApi
{
    public async Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        VaultCipherAttachment attachment,
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateAttachmentDownloadClient();

        var downloadResponse = await GetAttachmentDownloadResponseAsync(
            httpClient,
            accountContext,
            attachment,
            cancellationToken);

        if (!Uri.TryCreate(downloadResponse.Url, UriKind.Absolute, out var downloadUri))
        {
            throw new InvalidOperationException(
                $"Attachment download URL is not an absolute URI for attachment '{attachment.Id}'.");
        }

        using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        using var downloadHttpResponse = await httpClient.SendAsync(
            downloadRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        downloadHttpResponse.EnsureSuccess("Vault attachment download", cancellationToken);

        await using var stream = await downloadHttpResponse.Content.ReadAsStreamAsync(cancellationToken);
        await streamHandler.Invoke(stream);
    }

    private async Task<VaultCipherAttachmentDownloadResponse> GetAttachmentDownloadResponseAsync(
        HttpClient httpClient,
        BitwardenAccountContext accountContext,
        VaultCipherAttachment attachment,
        CancellationToken cancellationToken)
    {
        Uri requestUri = new(
            accountContext.Environment.ApiBase,
            $"/ciphers/{attachment.CipherId.Value}/attachment/{attachment.Id.Value}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Vault attachment metadata", cancellationToken);

        var downloadResponse = await response.Content.ReadFromJsonAsync(
            VaultJsonContext.ConfiguredDefault.VaultCipherAttachmentDownloadResponse,
            cancellationToken);

        return downloadResponse;
    }
}
