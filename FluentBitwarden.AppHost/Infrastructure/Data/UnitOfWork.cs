using FluentBitwarden.AppHost.Modules.Accounts.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using IsolationLevel = System.Data.IsolationLevel;

namespace FluentBitwarden.AppHost.Infrastructure.Data;

internal sealed class UnitOfWork : IDisposable
{
    public UnitOfWork(SqliteConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        Transaction = connection.BeginTransaction(isolationLevel, deferred: true);

        AccountProfileRepository = new AccountProfileRepository(Transaction);
        AccountKeyMaterialRepository = new AccountKeyMaterialRepository(Transaction);
        WindowsHelloKeyStoreRepository = new WindowsHelloKeyStoreRepository(Transaction);
        VaultReaderRepository = new VaultReaderRepository(Transaction);
        VaultWriterRepository = new VaultWriterRepository(Transaction);
        RefreshTokenRepository = new RefreshTokenRepository(Transaction);
    }

    private readonly SqliteConnection _connection;
    private bool _complete;

    public SqliteTransaction Transaction { get; }

    public IAccountProfileRepository AccountProfileRepository { get; }
    public AccountKeyMaterialRepository AccountKeyMaterialRepository { get; }
    public WindowsHelloKeyStoreRepository WindowsHelloKeyStoreRepository { get; }
    public VaultReaderRepository VaultReaderRepository { get; }
    public VaultWriterRepository VaultWriterRepository { get; }
    public RefreshTokenRepository RefreshTokenRepository { get; }

    public void SaveChanges()
    {
        Transaction.Commit();
        _complete = true;
    }

    public void Dispose()
    {
        if (!_complete)
        {
            Transaction.Rollback();
        }

        Transaction.Dispose();
        _connection.Dispose();
    }
}
