using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Infrastructure;

internal static partial class AppHostLog
{
    [LoggerMessage(EventId = 1500, Level = LogLevel.Error, Message = "Unhandled exception in the AppHost process.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);
}
