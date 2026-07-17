using Microsoft.Extensions.Logging;

namespace FluentBitwarden.Platform.SiteIcons;

internal static partial class SiteIconLog
{
    [LoggerMessage(EventId = 1100, Level = LogLevel.Trace, Message = "Site icon cache preload was canceled.")]
    public static partial void PreloadCanceled(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Error, Message = "Site icon cache preload failed.")]
    public static partial void PreloadFailed(this ILogger logger, Exception exception);
}
