using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Storage;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Storage;
using FluentBitwarden.Modules.Vault.Repositories;
using Microsoft.Data.Sqlite;
using IsolationLevel = System.Data.IsolationLevel;

namespace FluentBitwarden.AppHost.Infrastructure.Data;

internal sealed class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(SqliteConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        Transaction = connection.BeginTransaction(isolationLevel, deferred: true);

        AccountProfileRepository = new AccountProfileRepository(Transaction);
        AccountKeyMaterialRepository = new AccountKeyMaterialRepository(Transaction);
        VaultReaderRepository = new VaultReaderRepository(Transaction);
        SecureRefreshTokenStore = new SecureRefreshTokenStore(Transaction);
    }

    private readonly SqliteConnection _connection;
    private bool _complete;

    public SqliteTransaction Transaction { get; }

    public AccountProfileRepository AccountProfileRepository { get; }
    public AccountKeyMaterialRepository AccountKeyMaterialRepository { get; }
    public VaultReaderRepository VaultReaderRepository { get; }
    public SecureRefreshTokenStore SecureRefreshTokenStore { get; }

    public void SaveChanges()
    {
        Transaction.Commit();
        _complete = true;
    }

    public void Dispose()
    {
        if (!_complete)
            Transaction.Rollback();

        Transaction.Dispose();

        _connection.Dispose();
    }
}