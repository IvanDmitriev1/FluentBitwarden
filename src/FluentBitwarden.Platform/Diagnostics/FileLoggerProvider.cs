using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Platform.Diagnostics;

internal sealed class FileLoggerProvider(string logName) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.Ordinal);
    private readonly FileLogSink _sink = new(logName);

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, static (category, sink) => new FileLogger(category, sink), _sink);

    public void Dispose()
    {
        _loggers.Clear();
        _sink.Dispose();
    }
}
