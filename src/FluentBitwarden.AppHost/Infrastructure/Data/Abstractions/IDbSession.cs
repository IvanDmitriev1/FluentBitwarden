using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

internal interface IDbSession
{
    SqliteConnection Connection { get; }
    SqliteTransaction? Transaction { get; }
}
