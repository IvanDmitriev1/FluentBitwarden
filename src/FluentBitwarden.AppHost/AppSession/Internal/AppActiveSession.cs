using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.AppSession.Internal;

internal sealed record AppActiveSession(
    AccountProfile Account,
    UnlockedUserKey UserKey,
    IUnlockedVault UnlockedVault) : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public void Dispose()
    {
        _cts.Cancel();

        UserKey.Dispose();
        UnlockedVault.Dispose();

        _cts.Dispose();
    }

    public UnlockedSessionView ToUnlockedSessionView() =>
        new(Account, UnlockedVault, _cts.Token);
}
