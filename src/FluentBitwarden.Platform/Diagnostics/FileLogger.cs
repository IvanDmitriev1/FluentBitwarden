using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Platform.Diagnostics;

internal sealed class FileLogger(string category, FileLogSink sink) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    // Severity filtering is applied by the logging builder filter configured in AddAppLogging.
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        sink.Write(BuildEntry(logLevel, eventId, formatter(state, exception), exception));
    }

    private string BuildEntry(LogLevel logLevel, EventId eventId, string message, Exception? exception)
    {
        StringBuilder builder = new();

        builder.Append(DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture))
            .Append(" [").Append(logLevel.GetLevelName()).Append("] ")
            .Append(category);

        if (eventId.Id != 0)
        {
            builder.Append('(').Append(eventId.Id.ToString(CultureInfo.InvariantCulture)).Append(')');
        }

        builder.Append(": ").AppendLine(message);

        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        return builder.ToString();
    }
}
