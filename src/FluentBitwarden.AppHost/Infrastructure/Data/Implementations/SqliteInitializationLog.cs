using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal static partial class SqliteInitializationLog
{
    [LoggerMessage(
        EventId = 1200,
        EventName = "MigrationJournalRepaired",
        Level = LogLevel.Warning,
        Message = "Repaired the migration journal entry for {ScriptName} after verifying the existing schema.")]
    public static partial void MigrationJournalRepaired(
        ILogger logger,
        string scriptName);
}
