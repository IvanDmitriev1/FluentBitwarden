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
        Transaction = connection.BeginTransaction(isolationLevel);

        AccountRepository = new AccountRepository(Transaction);
        AccountDecryptionRepository = new AccountDecryptionRepository(Transaction);
        VaultRepository = new VaultRepository(Transaction);
    }

    private readonly SqliteConnection _connection;
    private bool _complete;

    public SqliteTransaction Transaction { get; }

    public IAccountRepository AccountRepository { get; }
    public IAccountDecryptionRepository AccountDecryptionRepository { get; }
    public IVaultRepository VaultRepository { get; }

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