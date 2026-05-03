using System.Net;

namespace BitwardenApi.Shared.Transport;

public sealed class ApiStreamResponse : IDisposable
{
    public static async Task<ApiStreamResponse> CreateAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new ApiStreamResponse(response, stream);
    }

    private ApiStreamResponse(HttpResponseMessage response, Stream content)
    {
        _response = response;
        StatusCode = response.StatusCode;
        Content = content;
    }

    private readonly HttpResponseMessage _response;
    private bool _disposed;

    public HttpStatusCode StatusCode { get; }
    public Stream Content { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Content.Dispose();
        _response.Dispose();
    }
}
