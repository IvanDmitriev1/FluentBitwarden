using BitwaredApi.Abstractions;

namespace BitwaredApi.Services;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        => new(Task.Delay(delay, cancellationToken));
}
