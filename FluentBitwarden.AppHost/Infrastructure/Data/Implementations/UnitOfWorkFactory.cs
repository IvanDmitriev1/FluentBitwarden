using System.Data;
using FluentBitwarden.AppHost.Infrastructure.Data;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Implementations;

internal sealed class UnitOfWorkFactory(ISqliteConnectionFactory connectionFactory) : IUnitOfWorkFactory
{
    public UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var connection = connectionFactory.OpenConnection();
        return new UnitOfWork(connection, isolationLevel);
    }
}