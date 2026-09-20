using System.Reflection;
using Dapper;
using DbUp;
using DbUp.Sqlite.Helpers;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class SqliteInitializationService(ISqliteConnectionFactory sqliteConnectionFactory)
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

        var result = upgradeEngine.PerformUpgrade();
        if (result.Successful)
            return;

        string scriptName = result.ErrorScript?.Name ?? "unknown script";
        throw new InvalidOperationException($"SQLite database migration failed. Script: {scriptName}.", result.Error);
    }
}
