using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Repositories;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Repositories;
using Microsoft.Data.Sqlite;
using IsolationLevel = System.Data.IsolationLevel;

namespace FluentBitwarden.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(SqliteConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        Transaction = connection.BeginTransaction(isolationLevel, deferred: true);

        AccountProfileRepository = new AccountProfileRepository(Transaction);
        AccountKeyMaterialRepository = new AccountKeyMaterialRepository(Transaction);
        VaultReaderRepository = new VaultReaderRepository(Transaction);
    }

    private readonly SqliteConnection _connection;
    private bool _complete;

    public SqliteTransaction Transaction { get; }

    public IAccountProfileRepository AccountProfileRepository { get; }
    public IAccountKeyMaterialRepository AccountKeyMaterialRepository { get; }
    public IVaultReaderRepository VaultReaderRepository { get; }

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