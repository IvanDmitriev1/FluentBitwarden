using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class DbUpDataInitializationService(ISqliteConnectionFactory connectionFactory) : IDataInitializationService
{
    public void Initialize()
    {
        var upgradeEngine = BuildUpgradeEngine();
        var result = upgradeEngine.PerformUpgrade();
        ThrowIfFailed(result, "SQLite database migration failed");
    }

    private UpgradeEngine BuildUpgradeEngine() =>
        DeployChanges.To
            .SqliteDatabase(connectionFactory.ConnectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                static resourceName =>
                    resourceName.Contains(".Migrations.", StringComparison.Ordinal) &&
                    resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .WithTransactionPerScript()
            .JournalToSqliteTable("SchemaVersions")
            .LogToTrace()
            .Build();

    private static void ThrowIfFailed(DatabaseUpgradeResult result, string message)
    {
        if (result.Successful)
        {
            return;
        }

        string scriptName = result.ErrorScript?.Name ?? "unknown script";
        throw new InvalidOperationException($"{message}. Script: {scriptName}.", result.Error);
    }
}
