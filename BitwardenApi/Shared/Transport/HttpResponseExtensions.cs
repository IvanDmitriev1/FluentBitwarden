using System.Text;
using BitwardenApi.Shared.Exceptions;

namespace BitwardenApi.Shared.Transport;

internal static class HttpResponseExtensions
{
    public static void EnsureSuccess(
        this HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw CreateFailureExceptionAsync(response, operation);
    }

    public static Task<ApiStreamResponse> CreateStreamResponseAsync(
        this HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return ApiStreamResponse.CreateAsync(response);

        var exception = CreateFailureExceptionAsync(response, operation);
        response.Dispose();
        throw exception;
    }

    public static BitwardenApiException CreateFailureExceptionAsync(
        HttpResponseMessage response,
        string operation)
    {
        using var streamReader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8);
        string body = streamReader.ReadToEnd();

        string message = $"{operation} failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).";
        if (!string.IsNullOrWhiteSpace(body))
        {
            message = $"{message} {body}";
        }

        return new BitwardenApiException(
            operation,
            message,
            response.StatusCode,
            body,
            response.RequestMessage?.RequestUri);
    }
}
