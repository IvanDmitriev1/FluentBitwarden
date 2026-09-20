using BitwardenApi.Primitives.Ids;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IUnlockedVault : IDisposable
{
    public UserId UserId { get; }
}
