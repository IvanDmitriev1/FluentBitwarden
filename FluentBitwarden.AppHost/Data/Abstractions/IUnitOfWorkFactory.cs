using System.Data;

namespace FluentBitwarden.Data.Abstractions;

internal interface IUnitOfWorkFactory
{
    UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}