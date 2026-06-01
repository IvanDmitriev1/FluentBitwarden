using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data;

public abstract class BaseRepository(SqliteTransaction transaction)
{
    public SqliteConnection Connection => transaction.Connection ??
                                          throw new ArgumentNullException(nameof(transaction.Connection));

    public SqliteTransaction Transaction => transaction;
}