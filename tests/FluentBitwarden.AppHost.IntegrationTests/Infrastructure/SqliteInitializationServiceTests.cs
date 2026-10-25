using System.Globalization;
using Dapper;
using Microsoft.Extensions.Logging;
using static FluentBitwarden.AppHost.IntegrationTests.Infrastructure.SqliteInitializationTestData;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

[DapperAot]
public sealed class SqliteInitializationServiceTests
{
    [Fact]
    public void Initialize_applies_five_migrations_to_a_fresh_database_and_is_idempotent()
    {
        using var database = new TemporarySqliteDatabase();
        var service = CreateService(database);

        service.Initialize();
        service.Initialize();

        using var connection = database.OpenConnection();
        Assert.Equal(
            AllCurrentMigrations,
            connection.Query<string>("SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'vault_sync_state');"));
    }

    [Fact]
    public void Initialize_reconciles_four_real_timestamped_aliases_once_without_replaying_their_migrations()
    {
        string[] appliedTimes =
        [
            "2026-07-01 01:00:00",
            "2026-07-01 02:00:00",
            "2026-07-01 03:00:00",
            "2026-07-02 01:00:00"
        ];
        using var database = new TemporarySqliteDatabase();
        using (var connection = database.OpenConnection())
        {
            CreateJournal(connection);
            for (var index = 0; index < LegacyMigrationsBeforeVaultSyncState.Length; index++)
            {
                connection.Execute(
                    "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
                    new { ScriptName = LegacyMigrationsBeforeVaultSyncState[index], Applied = appliedTimes[index] });
            }
        }

        var service = CreateService(database);
        service.Initialize();
        service.Initialize();

        using var result = database.OpenConnection();
        Assert.Equal(
            LegacyMigrationsBeforeVaultSyncState.Concat(AllCurrentMigrations)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray(),
            result.Query<string>("SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
        Assert.Equal(0, result.ExecuteScalar<int>("SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'account_profiles');"));
        Assert.Equal(1, result.ExecuteScalar<int>("SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'vault_sync_state');"));

        for (var index = 0; index < LegacyMigrationsBeforeVaultSyncState.Length; index++)
        {
            string legacyName = LegacyMigrationsBeforeVaultSyncState[index];
            string currentName = CurrentMigrationsBeforeVaultSyncState[index];
            string expectedApplied = appliedTimes[index];

            Assert.Equal(expectedApplied, result.ExecuteScalar<string>(
                "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
                new { ScriptName = legacyName }));
            Assert.Equal(expectedApplied, result.ExecuteScalar<string>(
                "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
                new { ScriptName = currentName }));
        }
    }

    [Fact]
    public void Reconcile_derives_aliases_for_unconfigured_prefixes_and_copies_the_earliest_applied_time_once()
    {
        const string firstLegacyName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607020001_create_synthetic_schema.sql";
        const string earliestLegacyName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010001_create_synthetic_schema.sql";
        const string currentName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.777_create_synthetic_schema.sql";
        const string earliestApplied = "2026-07-01 01:00:00";

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = firstLegacyName, Applied = "2026-07-02 01:00:00" });
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = earliestLegacyName, Applied = earliestApplied });

        IReadOnlyList<string> firstRepairs = SqliteMigrationJournalReconciler.Reconcile(
            connection,
            [(currentName, "-- no-op")]);
        IReadOnlyList<string> secondRepairs = SqliteMigrationJournalReconciler.Reconcile(
            connection,
            [(currentName, "-- no-op")]);

        Assert.Empty(firstRepairs);
        Assert.Empty(secondRepairs);
        Assert.Equal(earliestApplied, connection.ExecuteScalar<string>(
            "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = currentName }));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = currentName }));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = firstLegacyName }));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = earliestLegacyName }));
    }

    [Fact]
    public void Reconcile_preserves_the_applied_time_of_an_existing_current_name()
    {
        const string legacyName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_preserved_current_schema.sql";
        const string currentName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.888_preserved_current_schema.sql";
        const string currentApplied = "2026-07-03 01:00:00";

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = legacyName, Applied = "2026-07-01 01:00:00" });
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = currentName, Applied = currentApplied });

        SqliteMigrationJournalReconciler.Reconcile(connection, [(currentName, "-- no-op")]);

        Assert.Equal(currentApplied, connection.ExecuteScalar<string>(
            "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = currentName }));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = currentName }));
    }

    [Fact]
    public void Reconcile_ignores_malformed_names_and_suffixes_that_differ_by_case()
    {
        string[] legacyNames =
        [
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.20260703001_bad_history_short.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.2026070300001_bad_history_long.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.２０２６０７０３０００１_bad_history_unicode.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_bad_current_short.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_bad_current_long.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_bad_current_separator.sql",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_bad_non_sql.txt",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_bad_history_non_sql.txt",
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_bad_suffix_case.sql"
        ];
        (string Name, string Contents)[] discoveredScripts =
        [
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_bad_history_short.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.002_bad_history_long.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.003_bad_history_unicode.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.01_bad_current_short.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.0001_bad_current_long.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001bad_current_separator.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_bad_non_sql.txt", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.004_bad_history_non_sql.sql", "-- no-op"),
            ("FluentBitwarden.AppHost.Infrastructure.Data.Migrations.005_bad_suffix_Case.sql", "-- no-op")
        ];

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        foreach (string legacyName in legacyNames)
        {
            connection.Execute(
                "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
                new { ScriptName = legacyName, Applied = "2026-07-03 01:00:00" });
        }

        IReadOnlyList<string> repaired = SqliteMigrationJournalReconciler.Reconcile(connection, discoveredScripts);

        Assert.Empty(repaired);
        Assert.Equal(
            legacyNames.OrderBy(static name => name, StringComparer.Ordinal).ToArray(),
            connection.Query<string>("SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
    }

    [Fact]
    public void Reconcile_requires_matching_migration_namespaces()
    {
        const string legacyName =
            "LegacyBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_same_suffix.sql";
        const string currentName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.777_same_suffix.sql";

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = legacyName, Applied = "2026-07-03 01:00:00" });

        IReadOnlyList<string> repaired = SqliteMigrationJournalReconciler.Reconcile(
            connection,
            [(currentName, "-- no-op")]);

        Assert.Empty(repaired);
        Assert.Null(connection.ExecuteScalar<string>(
            "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = currentName }));
        Assert.Equal(legacyName, connection.ExecuteScalar<string>(
            "SELECT ScriptName FROM SchemaVersions;"));
    }

    [Fact]
    public void Reconcile_rejects_ambiguous_suffixes_before_inserting_any_aliases()
    {
        const string legacyAmbiguousName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_ambiguous_suffix.sql";
        const string legacyUnambiguousName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030002_unambiguous_suffix.sql";
        const string firstAmbiguousCurrentName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_ambiguous_suffix.sql";
        const string secondAmbiguousCurrentName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.002_ambiguous_suffix.sql";
        const string unambiguousCurrentName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.003_unambiguous_suffix.sql";

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = legacyAmbiguousName, Applied = "2026-07-03 01:00:00" });
        connection.Execute(
            "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
            new { ScriptName = legacyUnambiguousName, Applied = "2026-07-03 02:00:00" });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SqliteMigrationJournalReconciler.Reconcile(
                connection,
                [
                    (firstAmbiguousCurrentName, "-- no-op"),
                    (secondAmbiguousCurrentName, "-- no-op"),
                    (unambiguousCurrentName, "-- no-op")
                ]));

        Assert.Contains("multiple discovered scripts", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ambiguous_suffix.sql", exception.Message, StringComparison.Ordinal);
        Assert.Equal(
            new[] { legacyAmbiguousName, legacyUnambiguousName }.OrderBy(static name => name, StringComparer.Ordinal).ToArray(),
            connection.Query<string>("SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
        Assert.Null(connection.ExecuteScalar<string>(
            "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = unambiguousCurrentName }));
    }

    [Fact]
    public void Initialize_repairs_a_missing_journal_entry_and_preserves_the_existing_schema_rows_and_applied_values()
    {
        using var database = new TemporarySqliteDatabase();
        using (var connection = database.OpenConnection())
        {
            CreateJournal(connection);
            InsertJournalEntries(
                connection,
                LegacyMigrationsBeforeVaultSyncState.Concat(CurrentMigrationsBeforeVaultSyncState));
            connection.Execute("CREATE TABLE account_profiles (user_id TEXT PRIMARY KEY NOT NULL COLLATE NOCASE);");
            connection.Execute("INSERT INTO account_profiles (user_id) VALUES ('user-1');");
            connection.Execute(GetMigrationSql(VaultSyncStateMigrationName)
                .Replace("CREATE TABLE", "CREATE\n    TABLE", StringComparison.Ordinal));
            connection.Execute("""
                INSERT INTO vault_sync_state (user_id, server_revision_date_unix_ms)
                VALUES ('user-1', 12345);
                """);
        }

        var logger = new RecordingLogger<SqliteInitializationService>(() =>
        {
            using var observed = database.OpenConnection();
            Assert.Equal(1, observed.ExecuteScalar<int>("""
                SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;
                """, new { ScriptName = VaultSyncStateMigrationName }));
        });
        var service = CreateService(database, logger);
        DateTime repairStartedAtUtc = DateTime.UtcNow;
        service.Initialize();

        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
        Assert.Equal(1200, logger.Entries[0].EventId.Id);
        Assert.Equal("MigrationJournalRepaired", logger.Entries[0].EventId.Name);
        Assert.Contains(VaultSyncStateMigrationName, logger.Entries[0].Message, StringComparison.Ordinal);

        service.Initialize();
        Assert.Single(logger.Entries);

        using var result = database.OpenConnection();
        Assert.Equal(
            LegacyMigrationsBeforeVaultSyncState.Concat(AllCurrentMigrations)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray(),
            result.Query<string>("SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
        Assert.Equal(1, result.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;
            """, new { ScriptName = VaultSyncStateMigrationName }));
        DateTime repairedAtUtc = DateTime.Parse(
            result.ExecuteScalar<string>(
                "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
                new { ScriptName = VaultSyncStateMigrationName })!,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        Assert.InRange(repairedAtUtc, repairStartedAtUtc.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));

        string[] preexistingMigrations =
            LegacyMigrationsBeforeVaultSyncState.Concat(CurrentMigrationsBeforeVaultSyncState).ToArray();
        for (var index = 0; index < preexistingMigrations.Length; index++)
        {
            Assert.Equal($"legacy-applied-{index}", result.ExecuteScalar<string>(
                "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
                new { ScriptName = preexistingMigrations[index] }));
        }

        Assert.Equal(12345L, result.ExecuteScalar<long>("""
            SELECT server_revision_date_unix_ms FROM vault_sync_state WHERE user_id = 'user-1';
            """));
    }

    [Fact]
    public void Reconcile_repairs_a_missing_multi_table_migration_entry()
    {
        const string scriptName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_create_two_reconciliation_tables.sql";
        const string scriptSql = """
            CREATE TABLE reconciliation_first (value TEXT NOT NULL);
            CREATE TABLE reconciliation_second (value INTEGER NOT NULL);
            """;

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute(scriptSql);
        connection.Execute("INSERT INTO reconciliation_first (value) VALUES ('kept');");
        connection.Execute("INSERT INTO reconciliation_second (value) VALUES (42);");

        IReadOnlyList<string> repaired = SqliteMigrationJournalReconciler.Reconcile(
            connection,
            [(scriptName, scriptSql)]);

        Assert.Equal(new[] { scriptName }, repaired);
        Assert.Equal(new[] { scriptName }, connection.Query<string>(
            "SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM reconciliation_first WHERE value = 'kept';"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM reconciliation_second WHERE value = 42;"));
    }

    [Fact]
    public void Reconcile_repairs_migration_004_only_when_its_table_and_explicit_index_match()
    {
        const string scriptName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.004_create_vault_cipher_attachment_schema.sql";
        string scriptSql = GetMigrationSql(scriptName);

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute("""
            CREATE TABLE vault_cipher (
                user_id TEXT NOT NULL,
                cipher_id TEXT NOT NULL,
                PRIMARY KEY (user_id, cipher_id)
            );
            """);
        connection.Execute(scriptSql);

        IReadOnlyList<string> repaired = SqliteMigrationJournalReconciler.Reconcile(
            connection,
            [(scriptName, scriptSql)]);

        Assert.Equal(new[] { scriptName }, repaired);
        Assert.Equal(new[] { scriptName }, connection.Query<string>(
            "SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName COLLATE BINARY;").ToArray());
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'vault_cipher_attachment');"));
        Assert.Equal(1, connection.ExecuteScalar<int>("""
            SELECT EXISTS (
                SELECT 1 FROM sqlite_schema
                WHERE type = 'index' AND name = 'ix_vault_cipher_attachment_user_cipher_sort_order'
            );
            """));
    }

    [Fact]
    public void Reconcile_rejects_a_partially_present_migration_without_journaling_it()
    {
        const string scriptName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_create_two_reconciliation_tables.sql";
        const string scriptSql = """
            CREATE TABLE reconciliation_first (value TEXT NOT NULL);
            CREATE TABLE reconciliation_second (value INTEGER NOT NULL);
            """;

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute("CREATE TABLE reconciliation_first (value TEXT NOT NULL);");
        connection.Execute("INSERT INTO reconciliation_first (value) VALUES ('kept');");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SqliteMigrationJournalReconciler.Reconcile(connection, [(scriptName, scriptSql)]));

        Assert.Contains(scriptName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("reconciliation_second", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, connection.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;
            """, new { ScriptName = scriptName }));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM reconciliation_first WHERE value = 'kept';"));
    }

    [Fact]
    public void Reconcile_rejects_an_existing_object_with_the_expected_name_but_a_different_type()
    {
        const string scriptName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_create_reconciliation_table.sql";
        const string scriptSql = "CREATE TABLE reconciliation_conflict (value TEXT NOT NULL);";

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        connection.Execute("CREATE VIEW reconciliation_conflict AS SELECT 'kept' AS value;");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SqliteMigrationJournalReconciler.Reconcile(connection, [(scriptName, scriptSql)]));

        Assert.Contains(scriptName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("reconciliation_conflict", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SchemaVersions;"));
        Assert.Equal("kept", connection.ExecuteScalar<string>("SELECT value FROM reconciliation_conflict;"));
    }

    [Fact]
    public void Initialize_rejects_a_mismatched_existing_vault_sync_state_definition_without_journaling()
    {
        using var database = new TemporarySqliteDatabase();
        using (var connection = database.OpenConnection())
        {
            CreateJournal(connection);
            InsertJournalEntries(connection, CurrentMigrationsBeforeVaultSyncState);
            connection.Execute("""
                CREATE TABLE vault_sync_state (
                    user_id TEXT PRIMARY KEY NOT NULL,
                    server_revision_date_unix_ms INTEGER NOT NULL
                );
                """);
            connection.Execute("""
                INSERT INTO vault_sync_state (user_id, server_revision_date_unix_ms)
                VALUES ('user-1', 12345);
                """);
        }

        string? originalDefinition;
        using (var connection = database.OpenConnection())
        {
            originalDefinition = connection.ExecuteScalar<string>(
                "SELECT sql FROM sqlite_schema WHERE type = 'table' AND name = @TableName;",
                new { TableName = "vault_sync_state" });
        }

        var service = CreateService(database);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(service.Initialize);

        Assert.Contains("vault_sync_state", exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
        using var result = database.OpenConnection();
        Assert.Equal(0, result.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM SchemaVersions WHERE ScriptName = @ScriptName;
            """, new { ScriptName = VaultSyncStateMigrationName }));
        Assert.Equal(originalDefinition, result.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_schema WHERE type = 'table' AND name = @TableName;",
            new { TableName = "vault_sync_state" }));
        Assert.Equal(12345L, result.ExecuteScalar<long>("""
            SELECT server_revision_date_unix_ms FROM vault_sync_state WHERE user_id = 'user-1';
            """));
    }

    [Fact]
    public void Reconcile_does_not_automatically_repair_a_future_migration()
    {
        const string scriptName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.006_future_migration.sql";
        const string existingFutureJournalName =
            "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607030001_legacy_future_migration.sql";
        const string scriptSql = "CREATE TABLE future_migration_data (value TEXT NOT NULL);";

        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();
        CreateJournal(connection);
        InsertJournalEntries(connection, (string[])[existingFutureJournalName]);
        connection.Execute(scriptSql);
        connection.Execute("INSERT INTO future_migration_data (value) VALUES ('kept');");

        IReadOnlyList<string> repaired = SqliteMigrationJournalReconciler.Reconcile(
            connection,
            [(scriptName, scriptSql)]);

        Assert.Empty(repaired);
        Assert.Null(connection.ExecuteScalar<string>(
            "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = scriptName }));
        Assert.Equal("legacy-applied-0", connection.ExecuteScalar<string>(
            "SELECT Applied FROM SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = existingFutureJournalName }));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM future_migration_data WHERE value = 'kept';"));
    }

    [Fact]
    public void Initialize_preserves_the_failing_script_and_original_error()
    {
        using var database = new TemporarySqliteDatabase();
        using (var connection = database.OpenConnection())
        {
            connection.Execute("""
                CREATE TABLE SchemaVersions (
                    ScriptName TEXT NOT NULL CHECK (
                        ScriptName <> 'FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_create_account_schema.sql'
                    ),
                    Applied TEXT NOT NULL
                );
                """);
        }

        var service = CreateService(database);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(service.Initialize);

        Assert.Contains("001_create_account_schema.sql", exception.Message, StringComparison.Ordinal);
        Assert.IsType<SqliteException>(exception.InnerException);
    }

}
