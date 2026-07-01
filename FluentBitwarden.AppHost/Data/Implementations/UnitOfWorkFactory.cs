using System.Data;
using FluentBitwarden.AppHost.Data;
using FluentBitwarden.AppHost.Data.Abstractions;

namespace FluentBitwarden.AppHost.Data.Implementations;

internal sealed class UnitOfWorkFactory(ISqliteConnectionFactory connectionFactory) : IUnitOfWorkFactory
{
    public UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var connection = connectionFactory.OpenConnection();
        return new UnitOfWork(connection, isolationLevel);
    }
}