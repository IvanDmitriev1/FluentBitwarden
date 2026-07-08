namespace FluentBitwarden.AppHost.Infrastructure.Extensions;

internal static class DateTimeOffsetExtensions
{
    public static DateTimeOffset TruncateToSeconds(this DateTimeOffset value)
    {
        return new DateTimeOffset(
            value.UtcDateTime.Ticks - (value.UtcDateTime.Ticks % TimeSpan.TicksPerSecond),
            TimeSpan.Zero);
    }
}
