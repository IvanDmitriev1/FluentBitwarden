using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

[DapperAot]
internal static class SqliteInitializationTestData
{
    internal const string VaultSyncStateMigrationName =
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.005_create_vault_sync_state.sql";

    internal static readonly string[] CurrentMigrationsBeforeVaultSyncState =
    [
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.001_create_account_schema.sql",
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.002_create_vault_base_schema.sql",
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.003_create_vault_item_schema.sql",
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.004_create_vault_cipher_attachment_schema.sql"
    ];

    internal static readonly string[] AllCurrentMigrations =
    [
        .. CurrentMigrationsBeforeVaultSyncState,
        VaultSyncStateMigrationName
    ];

    internal static readonly string[] LegacyMigrationsBeforeVaultSyncState =
    [
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010001_create_account_schema.sql",
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010002_create_vault_base_schema.sql",
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607010003_create_vault_item_schema.sql",
        "FluentBitwarden.AppHost.Infrastructure.Data.Migrations.202607020001_create_vault_cipher_attachment_schema.sql"
    ];

    internal static void CreateJournal(SqliteConnection connection) => connection.Execute("""
        CREATE TABLE SchemaVersions (
            ScriptName TEXT NOT NULL,
            Applied TEXT NOT NULL
        );
        """);

    internal static SqliteInitializationService CreateService(
        TemporarySqliteDatabase database,
        ILogger<SqliteInitializationService>? logger = null) =>
        new(database.ConnectionFactory, logger ?? NullLogger<SqliteInitializationService>.Instance);

    internal static void InsertJournalEntries(SqliteConnection connection, IEnumerable<string> scriptNames)
    {
        var index = 0;
        foreach (string scriptName in scriptNames)
        {
            connection.Execute(
                "INSERT INTO SchemaVersions (ScriptName, Applied) VALUES (@ScriptName, @Applied);",
                new { ScriptName = scriptName, Applied = $"legacy-applied-{index}" });
            index++;
        }
    }

    internal static string GetMigrationSql(string scriptName)
    {
        using Stream stream = typeof(SqliteInitializationService).Assembly.GetManifestResourceStream(scriptName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

internal sealed class TemporarySqliteDatabase : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "FluentBitwarden.AppHost.IntegrationTests",
        Guid.NewGuid().ToString("N"));

    public TemporarySqliteDatabase()
    {
        Directory.CreateDirectory(_directoryPath);
        DatabasePath = Path.Combine(_directoryPath, "initialization.db");
        ConnectionFactory = new SqliteConnectionFactory(DatabasePath);
    }

    public string DatabasePath { get; }

    public SqliteConnectionFactory ConnectionFactory { get; }

    public SqliteConnection OpenConnection() => ConnectionFactory.OpenConnection();

    public void Dispose()
    {
        using (SqliteConnection connection = OpenConnection())
            SqliteConnection.ClearPool(connection);

        File.Delete(DatabasePath);
        File.Delete($"{DatabasePath}-wal");
        File.Delete($"{DatabasePath}-shm");
        Directory.Delete(_directoryPath);
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly Action? _onLog;

    public RecordingLogger(Action? onLog = null) => _onLog = onLog;

    public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, eventId, formatter(state, exception)));
        _onLog?.Invoke();
    }
}
