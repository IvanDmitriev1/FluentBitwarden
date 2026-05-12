using Microsoft.AspNetCore.SignalR.Client;

namespace BitwardenApi.Infrastructure.Notifications;

internal static class HubConnectionExtensions
{
    public static async Task StartWithRetryAsync(
        this HubConnection connection,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        var maxDelay = TimeSpan.FromSeconds(30);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await connection.StartAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch when (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                var nextSeconds = Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds);
                delay = TimeSpan.FromSeconds(nextSeconds);
            }
        }
    }

    private static bool IsPermanent404(AggregateException ex)
    {
        foreach (var inner in ex.Flatten().InnerExceptions)
        {
            if (inner is HttpRequestException http &&
                http.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true;
            }

            if (inner.Message.Contains("404", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
