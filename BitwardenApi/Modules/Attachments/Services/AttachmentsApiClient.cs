using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BitwardenApi.Modules.Attachments.Abstractions;
using BitwardenApi.Modules.Attachments.Models;
using BitwardenApi.Shared.Serialization;
using BitwardenApi.Shared.Transport;

namespace BitwardenApi.Modules.Attachments.Services;

public sealed class AttachmentsApiClient(HttpClient httpClient) : IAttachmentsApiClient
{
    public async Task<AttachmentUploadInit> StartUploadV2Async(
        StartUploadV2Request request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(request.Context.Environment.ApiBase, $"/ciphers/{request.CipherId.Value:D}/attachment/v2");
        using HttpRequestMessage requestMessage = new(HttpMethod.Post, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);
        StreamContent requestContent = new(request.AttachmentRequestJson);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestMessage.Content = requestContent;

        using HttpResponseMessage response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Attachments start upload v2", cancellationToken);

        AttachmentUploadInit? payload = await response.Content.ReadFromJsonAsync(
            BitwardenApiJsonContext.ConfiguredDefault.AttachmentUploadInit,
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
        Uri requestUri = new(
            request.Context.Environment.ApiBase,
            $"/ciphers/{request.CipherId.Value:D}/attachment/{Uri.EscapeDataString((string)request.AttachmentId.Value)}/renew");
        using HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken.Value);

        using HttpResponseMessage response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Attachments renew upload", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync(
            BitwardenApiJsonContext.ConfiguredDefault.AttachmentUploadRenewal,
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
        Uri requestUri = request.RequestUri.IsAbsoluteUri
            ? request.RequestUri
            : new Uri(request.Context.Environment.ApiBase, request.RequestUri);

        using MultipartFormDataContent multipart = new();

        if (request.FormFields is not null)
        {
            foreach (KeyValuePair<string, string> field in request.FormFields)
            {
                multipart.Add(new StringContent(field.Value, Encoding.UTF8), field.Key);
            }
        }

        StreamContent fileContent = new(request.File);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
        multipart.Add(fileContent, request.FilePartName, request.FileName);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        requestMessage.Content = multipart;

        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccess("Attachments multipart upload", cancellationToken);
    }

    public async Task<ApiStreamResponse> DownloadByTokenAsync(
        DownloadByTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri requestUri = request.RequestUri.IsAbsoluteUri
            ? request.RequestUri
            : new Uri(request.Context.Environment.ApiBase, request.RequestUri);

        HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.DownloadToken.Value);

        var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await response.CreateStreamResponseAsync("Attachments download by token", cancellationToken);
    }
}
