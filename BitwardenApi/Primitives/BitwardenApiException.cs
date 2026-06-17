using System.Net;

namespace BitwardenApi.Primitives;

public sealed class BitwardenApiException(
    string operation,
    string message,
    HttpStatusCode? statusCode = null,
    string? responseBody = null,
    Uri? requestUri = null,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Operation { get; } = operation;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
    public Uri? RequestUri { get; } = requestUri;
}
