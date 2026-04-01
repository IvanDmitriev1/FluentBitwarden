using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Data;

public abstract class BaseRepository(SqliteTransaction transaction)
{
    public SqliteConnection Connection => transaction.Connection ??
                                          throw new ArgumentNullException(nameof(transaction.Connection));

    public SqliteTransaction Transaction => transaction;
}