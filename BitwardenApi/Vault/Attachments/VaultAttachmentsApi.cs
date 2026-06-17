using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BitwardenApi.Vault.Attachments;

internal sealed class VaultAttachmentsApi(
    IHttpClientFactory httpClientFactory,
    IBitwardenEnvironmentAccessor environmentAccessor) : IVaultAttachmentsApi
{
    public async Task<AttachmentUploadInit> StartUploadV2Async(
        StartUploadV2Request request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(environmentAccessor.CurrentEnvironment.ApiBase, $"/ciphers/{request.CipherId.Value:D}/attachment/v2");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        using var requestContent = new StreamContent(request.AttachmentRequestJson);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestMessage.Content = requestContent;

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Attachments start upload v2", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync(
            VaultJsonContext.ConfiguredDefault.AttachmentUploadInit,
            cancellationToken);

        if (payload is null)
        {
            throw new InvalidDataException("Response JSON payload was empty.");
        }

        if (!payload.Url.IsAbsoluteUri)
        {
            throw new InvalidDataException("Attachment upload init response did not include a valid absolute URL.");
        }

        return payload;
    }

    public async Task<AttachmentUploadRenewal> RenewUploadAsync(
        RenewUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(
            environmentAccessor.CurrentEnvironment.ApiBase,
            $"/ciphers/{request.CipherId.Value:D}/attachment/{Uri.EscapeDataString(request.AttachmentId.Value)}/renew");

        using HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUri);
        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Attachments renew upload", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync(
            VaultJsonContext.ConfiguredDefault.AttachmentUploadRenewal,
            cancellationToken);

        if (payload is null)
        {
            throw new InvalidDataException("Response JSON payload was empty.");
        }

        if (!payload.Url.IsAbsoluteUri)
        {
            throw new InvalidDataException("Attachment upload renewal response did not include a valid absolute URL.");
        }

        return payload;
    }

    public async Task UploadMultipartAsync(
        UploadMultipartRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new Uri(environmentAccessor.CurrentEnvironment.ApiBase, request.RequestUri);
        using MultipartFormDataContent multipart = new();

        foreach (var field in request.FormFields)
        {
            multipart.Add(new StringContent(field.Value, System.Text.Encoding.UTF8), field.Key);
        }

        using var fileContent = new StreamContent(request.File);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
        multipart.Add(fileContent, "data", request.FileName);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        requestMessage.Content = multipart;

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Attachments multipart upload", cancellationToken);
    }

    public async Task DownloadByTokenAsync(
        DownloadByTokenRequest request,
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new Uri(environmentAccessor.CurrentEnvironment.ApiBase, request.RequestUri);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await streamHandler.Invoke(stream);
    }
}

