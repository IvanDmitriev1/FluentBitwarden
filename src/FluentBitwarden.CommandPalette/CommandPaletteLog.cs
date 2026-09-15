using Microsoft.Extensions.Logging;

namespace FluentBitwarden.CommandPalette;

internal static partial class CommandPaletteLog
{
    [LoggerMessage(EventId = 1600, Level = LogLevel.Error,
        Message = "Unhandled exception in the CommandPalette process.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);
}
