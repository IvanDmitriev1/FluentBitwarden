using System.Data;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class UnitOfWorkFactory(ISqliteConnectionFactory connectionFactory) : IUnitOfWorkFactory
{
    public UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var connection = connectionFactory.OpenConnection();
        return new UnitOfWork(connection, isolationLevel);
    }
}