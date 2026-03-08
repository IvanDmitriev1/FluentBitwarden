namespace BitwaredApi.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
