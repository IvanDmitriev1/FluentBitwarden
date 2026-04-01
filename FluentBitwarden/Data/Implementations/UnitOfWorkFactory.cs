using FluentBitwarden.Data.Abstractions;

namespace FluentBitwarden.Data.Implementations;

internal sealed class UnitOfWorkFactory(ISqliteConnectionFactory connectionFactory) : IUnitOfWorkFactory
{
    public UnitOfWork Create()
    {
        var connection = connectionFactory.OpenConnection();
        return new UnitOfWork(connection);
    }
}