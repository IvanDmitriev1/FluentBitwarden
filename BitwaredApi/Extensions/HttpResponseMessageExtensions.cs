using BitwaredApi.Abstractions.Exceptions;

namespace BitwaredApi.Extensions;

internal static class HttpResponseMessageExtensions
{
    public static async ValueTask EnsureBitwaredSuccessAsync(
        this HttpResponseMessage response,
        string responseSource,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ServerVersionMismatchException($"{responseSource} returned {(int)response.StatusCode}: {body}");
    }
}
