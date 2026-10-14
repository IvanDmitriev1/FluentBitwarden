using System.Reflection;
using Dapper;
using DbUp;
using DbUp.Sqlite.Helpers;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class SqliteInitializationService(
    ISqliteConnectionFactory sqliteConnectionFactory,
    ILogger<SqliteInitializationService> logger)
    : IDatabaseInitializationService
{
    public void Initialize()
    {
        using var connection = sqliteConnectionFactory.OpenConnection();
        var sharedConnection = new SharedConnection(connection);

        connection.Execute("PRAGMA journal_mode = WAL;");

        var upgradeEngine = DeployChanges.To
            .SqliteDatabase(sharedConnection)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                static resourceName =>
                    resourceName.Contains(".Migrations.", StringComparison.Ordinal) &&
                    resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .WithTransactionPerScript()
            .JournalToSqliteTable("SchemaVersions")
            .LogToTrace()
            .Build();

        var discoveredScripts = upgradeEngine.GetDiscoveredScripts()
            .Select(static script => (script.Name, script.Contents))
            .ToArray();

        foreach (string scriptName in SqliteMigrationJournalReconciler.Reconcile(connection, discoveredScripts))
        {
            SqliteInitializationLog.MigrationJournalRepaired(logger, scriptName);
        }

        var result = upgradeEngine.PerformUpgrade();
        if (result.Successful)
            return;

        string failedScriptName = result.ErrorScript?.Name ?? "unknown script";
        throw new InvalidOperationException(
            $"SQLite database migration failed. Script: {failedScriptName}.",
            result.Error);
    }
}
