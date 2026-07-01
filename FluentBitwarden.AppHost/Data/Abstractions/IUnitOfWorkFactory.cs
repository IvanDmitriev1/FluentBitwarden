using FluentBitwarden.AppHost.Data;
using System.Data;

namespace FluentBitwarden.AppHost.Data.Abstractions;

internal interface IUnitOfWorkFactory
{
    UnitOfWork Create(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}