using System.Net;
using System.Net.Http.Headers;

namespace BitwardenApi.Shared.Transport;

public sealed class ApiStreamResponse : IDisposable
{
    public static async Task<ApiStreamResponse> CreateAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return new ApiStreamResponse(response, stream);
    }

    private ApiStreamResponse(HttpResponseMessage response, Stream content)
    {
        _response = response;
        StatusCode = response.StatusCode;
        Content = content;
        Headers = FlattenHeaders(response.Headers, response.Content.Headers);
    }

    private readonly HttpResponseMessage _response;
    private bool _disposed;

    public HttpStatusCode StatusCode { get; }
    public Stream Content { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

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

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> FlattenHeaders(
        HttpResponseHeaders responseHeaders,
        HttpContentHeaders contentHeaders)
    {
        Dictionary<string, IReadOnlyList<string>> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in responseHeaders)
        {
            headers[pair.Key] = pair.Value as IReadOnlyList<string> ?? pair.Value.ToArray();
        }

        foreach (var pair in contentHeaders)
        {
            headers[pair.Key] = pair.Value as IReadOnlyList<string> ?? pair.Value.ToArray();
        }

        return headers;
    }
}
