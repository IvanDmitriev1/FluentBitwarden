using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Repositories;
using Microsoft.Data.Sqlite;
using System.Transactions;

namespace FluentBitwarden.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(SqliteConnection connection)
    {
        _connection = connection;
        Transaction = connection.BeginTransaction();

        AccountRepository = new AccountRepository(Transaction);
    }

    private readonly SqliteConnection _connection;
    private bool _complete;

    public SqliteTransaction Transaction { get; }

    public IAccountRepository AccountRepository { get; private set; }

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