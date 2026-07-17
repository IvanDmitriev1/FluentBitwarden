using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Infrastructure;

internal static partial class UiLog
{
    [LoggerMessage(EventId = 1400, Level = LogLevel.Error, Message = "Unhandled exception in the UI process.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1401, Level = LogLevel.Error, Message = "Unobserved task exception in the UI process.")]
    public static partial void UnobservedTaskException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Error, Message = "Page view model loading failed.")]
    public static partial void PageLoadFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1403, Level = LogLevel.Error, Message = "Vault cipher search failed.")]
    public static partial void CipherSearchFailed(this ILogger logger, Exception exception);
}
