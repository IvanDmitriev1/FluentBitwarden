namespace FluentBitwarden.Data.Abstractions;

public interface IUnitOfWorkFactory
{
    UnitOfWork Create();
}