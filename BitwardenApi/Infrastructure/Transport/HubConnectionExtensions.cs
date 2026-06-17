using Microsoft.AspNetCore.SignalR.Client;

namespace BitwardenApi.Infrastructure.Transport;

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
}

