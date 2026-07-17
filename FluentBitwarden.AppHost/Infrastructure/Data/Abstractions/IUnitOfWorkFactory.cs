using System.Data;

namespace FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

internal interface IUnitOfWorkFactory
{
    UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}
