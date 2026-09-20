using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class UnitOfWork(ISqliteConnectionFactory connectionFactory) : IUnitOfWork, IDisposable
{
    private SqliteConnection? _connection;
    private bool _disposed;


    public SqliteConnection Connection
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _connection ??= connectionFactory.OpenConnection();
        }
    }

    public SqliteTransaction? Transaction { get; private set; }


    public void Begin()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        Transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Transaction is null)
            throw new InvalidOperationException("No transaction is in progress.");

        try
        {
            Transaction.Commit();
        }
        finally
        {
            Transaction.Dispose();
            Transaction = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            Transaction?.Dispose();
        }
        finally
        {
            Transaction = null;

            _connection?.Dispose();
            _connection = null;
            _disposed = true;
        }
    }
}
