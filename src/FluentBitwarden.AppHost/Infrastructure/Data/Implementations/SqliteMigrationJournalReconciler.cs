using Dapper;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal static class SqliteMigrationJournalReconciler
{
    private const string MigrationResourceMarker = ".Migrations.";

    internal static IReadOnlyList<string> Reconcile(
        SqliteConnection connection, (string Name, string Contents)[] scripts)
    {
        if (!TableExists(connection, "SchemaVersions"))
            return [];

        ReconcileRenamedMigrations(connection, scripts);
        return ReconcileMissingMigrations(connection, scripts);
    }

    private static void ReconcileRenamedMigrations(
        SqliteConnection connection,
        IReadOnlyList<(string Name, string Contents)> discoveredScripts)
    {
        using var transaction = connection.BeginTransaction();
        JournalEntry[] journalEntries = connection.Query<JournalEntry>(
            "SELECT ScriptName, Applied FROM SchemaVersions;",
            transaction: transaction).ToArray();

        var currentScriptsBySuffix = new Dictionary<(string Namespace, string Suffix), string>();
        foreach ((string scriptName, _) in discoveredScripts)
        {
            if (!TryGetMigrationSuffix(scriptName, 3, out string resourceNamespace, out string suffix))
                continue;

            var key = (resourceNamespace, suffix);
            if (currentScriptsBySuffix.TryGetValue(key, out string? existingScriptName))
            {
                if (!string.Equals(existingScriptName, scriptName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Cannot reconcile migration journal aliases because multiple discovered scripts match suffix '{suffix}' in '{resourceNamespace}'.");
                }

                continue;
            }

            currentScriptsBySuffix.Add(key, scriptName);
        }

        HashSet<string> journaledScriptNames = journalEntries
            .Select(static entry => entry.ScriptName)
            .ToHashSet(StringComparer.Ordinal);

        var aliasesByCurrentName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JournalEntry entry in journalEntries)
        {
            if (!TryGetMigrationSuffix(entry.ScriptName, 12, out string resourceNamespace, out string suffix) ||
                !currentScriptsBySuffix.TryGetValue((resourceNamespace, suffix), out string? currentScriptName) ||
                journaledScriptNames.Contains(currentScriptName))
            {
                continue;
            }

            if (!aliasesByCurrentName.TryGetValue(currentScriptName, out string? earliestApplied) ||
                string.CompareOrdinal(entry.Applied, earliestApplied) < 0)
            {
                aliasesByCurrentName[currentScriptName] = entry.Applied;
            }
        }

        foreach ((string currentScriptName, string applied) in aliasesByCurrentName)
        {
            connection.Execute("""
                INSERT INTO SchemaVersions (ScriptName, Applied)
                VALUES (@ScriptName, @Applied);
                """, new { ScriptName = currentScriptName, Applied = applied }, transaction);
        }

        transaction.Commit();
    }

    private static bool TryGetMigrationSuffix(
        string scriptName,
        int prefixLength,
        out string resourceNamespace,
        out string suffix)
    {
        resourceNamespace = string.Empty;
        suffix = string.Empty;

        int markerIndex = scriptName.LastIndexOf(MigrationResourceMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
            return false;

        int fileNameStart = markerIndex + MigrationResourceMarker.Length;
        string fileName = scriptName[fileNameStart..];
        if (!fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
            fileName.Length <= prefixLength + 1 + ".sql".Length ||
            fileName[prefixLength] != '_')
        {
            return false;
        }

        for (var index = 0; index < prefixLength; index++)
        {
            if (fileName[index] is < '0' or > '9')
                return false;
        }

        resourceNamespace = scriptName[..fileNameStart];
        suffix = fileName[(prefixLength + 1)..];
        return true;
    }

    private static IReadOnlyList<string> ReconcileMissingMigrations(
        SqliteConnection connection,
        IEnumerable<(string Name, string Contents)> discoveredScripts)
    {
        (string Name, string Contents)[] scripts = discoveredScripts
            .Where(static script => IsEligibleScript(script.Name))
            .OrderBy(static script => script.Name, StringComparer.Ordinal)
            .ToArray();
        if (scripts.Length == 0)
            return [];

        Dictionary<string, IReadOnlyList<SchemaObject>> expectedObjectsByScript =
            GetObjectsCreatedByScripts(scripts);
        var repairedScripts = new List<string>();

        using var transaction = connection.BeginTransaction();
        if (!TableExists(connection, "SchemaVersions", transaction))
            return [];

        IReadOnlyList<SchemaObject> existingObjects = ReadSchema(connection, transaction);
        foreach ((string scriptName, _) in scripts)
        {
            if (IsMigrationJournaled(connection, scriptName, transaction))
                continue;

            IReadOnlyList<SchemaObject> expectedObjects = expectedObjectsByScript[scriptName];
            if (expectedObjects.Count == 0)
                continue;

            int existingObjectCount = 0;
            foreach (SchemaObject expectedObject in expectedObjects)
            {
                SchemaObject? existingObject = existingObjects.FirstOrDefault(
                    candidate => string.Equals(candidate.Name, expectedObject.Name, StringComparison.OrdinalIgnoreCase));
                if (existingObject is null)
                    continue;

                existingObjectCount++;
                if (!string.Equals(existingObject.Type, expectedObject.Type, StringComparison.OrdinalIgnoreCase) ||
                    existingObject.Sql is null || expectedObject.Sql is null ||
                    NormalizeSql(existingObject.Sql) != NormalizeSql(expectedObject.Sql))
                {
                    throw new InvalidOperationException(
                        $"Cannot repair the migration journal entry for '{scriptName}' because existing object '{expectedObject.Name}' does not match the migration.");
                }
            }

            if (existingObjectCount == 0)
                continue;

            if (existingObjectCount != expectedObjects.Count)
            {
                string missingObjectName = expectedObjects.First(expectedObject =>
                    !existingObjects.Any(candidate => string.Equals(
                        candidate.Name,
                        expectedObject.Name,
                        StringComparison.OrdinalIgnoreCase))).Name;
                throw new InvalidOperationException(
                    $"Cannot repair the migration journal entry for '{scriptName}' because expected object '{missingObjectName}' is missing while other objects from the migration exist.");
            }

            connection.Execute("""
                INSERT INTO SchemaVersions (ScriptName, Applied)
                VALUES (@ScriptName, CURRENT_TIMESTAMP);
                """, new { ScriptName = scriptName }, transaction);
            repairedScripts.Add(scriptName);
        }

        transaction.Commit();
        return repairedScripts;
    }

    private static Dictionary<string, IReadOnlyList<SchemaObject>> GetObjectsCreatedByScripts(
        IReadOnlyList<(string Name, string Contents)> scripts)
    {
        using var templateConnection = new SqliteConnection("Data Source=:memory:");
        templateConnection.Open();

        var objectsByScript = new Dictionary<string, IReadOnlyList<SchemaObject>>(StringComparer.Ordinal);
        foreach ((string scriptName, string contents) in scripts)
        {
            HashSet<string> previousObjects = ReadSchema(templateConnection)
                .Where(static schemaObject => IsMigrationObject(schemaObject.Type))
                .Select(static schemaObject => GetObjectKey(schemaObject.Type, schemaObject.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            templateConnection.Execute(contents);

            objectsByScript[scriptName] = ReadSchema(templateConnection)
                .Where(static schemaObject => IsMigrationObject(schemaObject.Type))
                .Where(schemaObject => !previousObjects.Contains(GetObjectKey(schemaObject.Type, schemaObject.Name)))
                .ToArray();
        }

        return objectsByScript;
    }

    private static IReadOnlyList<SchemaObject> ReadSchema(
        SqliteConnection connection,
        SqliteTransaction? transaction = null) =>
        connection.Query<SchemaObject>("""
            SELECT
                type AS Type,
                name AS Name,
                sql AS Sql
            FROM sqlite_schema
            WHERE type IN ('table', 'index', 'view', 'trigger')
              AND substr(name, 1, 7) COLLATE NOCASE <> 'sqlite_';
            """, transaction: transaction).ToArray();

    private static bool IsEligibleScript(string scriptName)
    {
        int markerIndex = scriptName.LastIndexOf(MigrationResourceMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
            return false;

        string migrationFileName = scriptName[(markerIndex + MigrationResourceMarker.Length)..];
        return migrationFileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
               (migrationFileName.StartsWith("001_", StringComparison.Ordinal) ||
                migrationFileName.StartsWith("002_", StringComparison.Ordinal) ||
                migrationFileName.StartsWith("003_", StringComparison.Ordinal) ||
                migrationFileName.StartsWith("004_", StringComparison.Ordinal) ||
                migrationFileName.StartsWith("005_", StringComparison.Ordinal));
    }

    private static string GetObjectKey(string type, string name) => $"{type}\0{name}";

    private static bool IsMigrationObject(string type) =>
        string.Equals(type, "table", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "index", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSql(string sql) =>
        string.Join(' ', sql.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).TrimEnd(';');

    private static bool TableExists(SqliteConnection connection, string tableName) =>
        connection.ExecuteScalar<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM sqlite_schema
                WHERE type = 'table' AND name = @TableName COLLATE NOCASE
            );
            """, new { TableName = tableName });

    private static bool TableExists(
        SqliteConnection connection,
        string tableName,
        SqliteTransaction transaction) =>
        connection.ExecuteScalar<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM sqlite_schema
                WHERE type = 'table' AND name = @TableName COLLATE NOCASE
            );
            """, new { TableName = tableName }, transaction);

    private static bool IsMigrationJournaled(
        SqliteConnection connection,
        string scriptName,
        SqliteTransaction transaction) =>
        connection.ExecuteScalar<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM SchemaVersions WHERE ScriptName = @ScriptName
            );
            """, new { ScriptName = scriptName }, transaction);

    public sealed record JournalEntry(string ScriptName, string Applied);

    public sealed record SchemaObject(string Type, string Name, string? Sql);
}
