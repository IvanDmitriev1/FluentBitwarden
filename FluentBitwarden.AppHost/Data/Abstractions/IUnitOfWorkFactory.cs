using System.Data;

namespace FluentBitwarden.Data.Abstractions;

public interface IUnitOfWorkFactory
{
    UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}