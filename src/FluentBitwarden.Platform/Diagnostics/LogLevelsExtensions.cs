using FluentBitwarden.Contracts.Settings.Models;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Platform.Diagnostics;

internal static class LogLevelsExtensions
{
    public static bool IsEnabled(LogLevels enabled, LogLevel level) => level switch
    {
        LogLevel.Trace or LogLevel.Debug => enabled.HasFlag(LogLevels.Trace),
        LogLevel.Information => enabled.HasFlag(LogLevels.Information),
        LogLevel.Warning => enabled.HasFlag(LogLevels.Warning),
        LogLevel.Error or LogLevel.Critical => enabled.HasFlag(LogLevels.Error),
        _ => false,
    };

    public static string GetLevelName(this LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        _ => "NONE",
    };
}
