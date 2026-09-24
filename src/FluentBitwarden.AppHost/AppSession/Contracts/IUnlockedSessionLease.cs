using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IUnlockedSessionLease : IDisposable
{
    public AccountProfile Account { get; }
    public IUnlockedVault Vault { get; }
    public UnlockedUserKey UnlockedUserKey { get; }
}
