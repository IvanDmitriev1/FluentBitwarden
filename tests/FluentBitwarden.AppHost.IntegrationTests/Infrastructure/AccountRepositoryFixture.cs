namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class AccountRepositoryFixture
{
    public AccountRepositoryTestDatabase CreateDatabase() => new();
}

public sealed class AccountRepositoryTestDatabase : IDisposable
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
            new SqliteInitializationService(_connectionFactory).Initialize();
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

    public SqliteConnection OpenConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _connectionFactory.OpenConnection();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        SqliteConnection.ClearAllPools();
        File.Delete(DatabasePath);
        File.Delete($"{DatabasePath}-wal");
        File.Delete($"{DatabasePath}-shm");
        Directory.Delete(_directoryPath);
    }
}
