using Microsoft.Extensions.Logging.Abstractions;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal sealed class AccountRepositoryTestDatabase : IDisposable
{
    private readonly string _directoryPath;
    private readonly SqliteConnectionFactory _connectionFactory;
    private bool _disposed;

    public AccountRepositoryTestDatabase()
    {
        _directoryPath = Path.Combine(
            Path.GetTempPath(),
            "FluentBitwarden.AppHost.IntegrationTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directoryPath);

        DatabasePath = Path.Combine(_directoryPath, "account.db");
        _connectionFactory = new SqliteConnectionFactory(DatabasePath);

        try
        {
            new SqliteInitializationService(
                _connectionFactory,
                NullLogger<SqliteInitializationService>.Instance).Initialize();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string DatabasePath { get; }

    internal UnitOfWork CreateUnitOfWork()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new UnitOfWork(_connectionFactory);
    }

    internal SqliteConnection OpenConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _connectionFactory.OpenConnection();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        using (SqliteConnection connection = _connectionFactory.OpenConnection())
            SqliteConnection.ClearPool(connection);

        File.Delete(DatabasePath);
        File.Delete($"{DatabasePath}-wal");
        File.Delete($"{DatabasePath}-shm");
        Directory.Delete(_directoryPath);
    }
}
