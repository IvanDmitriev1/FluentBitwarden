using System.Net;

namespace BitwaredApi.Http.DelegatingHandlers;

internal sealed class RetryHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        HttpRequestMessage currentRequest = request;

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                HttpResponseMessage response = await base.SendAsync(currentRequest, cancellationToken).ConfigureAwait(false);

                if (attempt >= 2 || !IsTransient(response.StatusCode))
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < 2)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150 * (attempt + 1)), cancellationToken).ConfigureAwait(false);
            currentRequest = await CloneAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.RequestTimeout
            || statusCode == (HttpStatusCode)429
            || (int)statusCode >= 500;

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpRequestMessage clone = new(request.Method, request.RequestUri);

        if (request.Options.TryGetValue(HttpRequestOptionKeys.SkipAuthorization, out bool skipAuthorization))
        {
            clone.Options.Set(HttpRequestOptionKeys.SkipAuthorization, skipAuthorization);
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            byte[] bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            clone.Content = new ByteArrayContent(bytes);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        clone.Version = request.Version;
        clone.VersionPolicy = request.VersionPolicy;
        return clone;
    }
}
