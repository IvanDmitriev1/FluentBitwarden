using System.Net.Http.Json;
using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault.Attachments;

internal sealed class VaultCipherAttachmentApi(IHttpClientFactory httpClientFactory) : IVaultCipherAttachmentApi
{
    public async Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        VaultCipherAttachment attachment,
        VaultCipherAttachmentStreamHandler streamHandler,
        CancellationToken cancellationToken = default)
    {
        attachment.CipherId.ThrowIfEmpty();
        attachment.Id.ThrowIfEmpty();

        var downloadResponse = await GetAttachmentDownloadResponseAsync(
            accountContext,
            attachment,
            cancellationToken);

        if (!Uri.TryCreate(downloadResponse.Url, UriKind.Absolute, out var downloadUri))
        {
            throw new InvalidOperationException(
                $"Attachment download URL is not an absolute URI for attachment '{attachment.Id}'.");
        }

        // The download URL is a pre-signed third-party storage URL; it is fetched with a bare client
        // that carries no Bitwarden authorization header, so the access token is never leaked to it.
        using var downloadClient = httpClientFactory.CreateAttachmentDownloadClient();
        using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        using var downloadHttpResponse = await downloadClient.SendAsync(
            downloadRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        downloadHttpResponse.EnsureSuccess("Vault attachment download", cancellationToken);

        await using var stream = await downloadHttpResponse.Content.ReadAsStreamAsync(cancellationToken);
        await streamHandler.Invoke(stream, downloadResponse.ProtectedAttachmentKey);
    }

    private async Task<VaultCipherAttachmentDownloadResponse> GetAttachmentDownloadResponseAsync(
        BitwardenAccountContext accountContext,
        VaultCipherAttachment attachment,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(
            accountContext.Environment.ApiBase,
            $"/ciphers/{attachment.CipherId.Value}/attachment/{attachment.Id.Value}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

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
