using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IUnlockedSessionLease : IDisposable
{
    public AccountProfile Account { get; }
    public IUnlockedVault Vault { get; }
}
