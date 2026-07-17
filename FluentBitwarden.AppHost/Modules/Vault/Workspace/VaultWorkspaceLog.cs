using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

internal static partial class VaultWorkspaceLog
{
    [LoggerMessage(EventId = 1200, Level = LogLevel.Error, Message = "Vault sync failed.")]
    public static partial void SyncFailed(this ILogger logger, Exception exception);
}
