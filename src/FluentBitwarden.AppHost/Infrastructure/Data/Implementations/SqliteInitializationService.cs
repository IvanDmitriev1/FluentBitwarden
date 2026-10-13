using System.Reflection;
using Dapper;
using DbUp;
using DbUp.Sqlite.Helpers;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class SqliteInitializationService(ISqliteConnectionFactory sqliteConnectionFactory)
    : IDatabaseInitializationService
{
    private static readonly (string LegacyName, string CurrentName)[] RenamedMigrationScripts =
    [
        (
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010001_create_account_schema.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_create_account_schema.sql"),
        (
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010002_create_vault_base_schema.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.002_create_vault_base_schema.sql"),
        (
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010003_create_vault_item_schema.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.003_create_vault_item_schema.sql"),
        (
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607020001_create_vault_cipher_attachment_schema.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.004_create_vault_cipher_attachment_schema.sql")
    ];

    public void Initialize()
    {
        using var connection = sqliteConnectionFactory.OpenConnection();
        var sharedConnection = new SharedConnection(connection);

        connection.Execute("PRAGMA journal_mode = WAL;");
        ReconcileRenamedMigrations(connection);

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

    private static void ReconcileRenamedMigrations(SqliteConnection connection)
    {
        const string schemaVersionsExistsSql =
            "SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'SchemaVersions');";
        if (!connection.ExecuteScalar<bool>(schemaVersionsExistsSql))
            return;

        const string insertCurrentScriptNameSql = """
            INSERT INTO SchemaVersions (ScriptName, Applied)
            SELECT @CurrentScriptName, MIN(Applied)
            FROM SchemaVersions
            WHERE ScriptName = @LegacyScriptName
            HAVING COUNT(*) > 0
               AND NOT EXISTS (
                   SELECT 1 FROM SchemaVersions WHERE ScriptName = @CurrentScriptName
               );
            """;

        using var transaction = connection.BeginTransaction();
        foreach ((string legacyName, string currentName) in RenamedMigrationScripts)
        {
            connection.Execute(
                insertCurrentScriptNameSql,
                new { CurrentScriptName = currentName, LegacyScriptName = legacyName },
                transaction);
        }

        transaction.Commit();
    }
}
