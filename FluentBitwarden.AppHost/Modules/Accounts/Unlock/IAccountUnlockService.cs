using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal interface IAccountUnlockService
{
    AccountUnlockResult Unlock(AccountUnlockRequest request);
}
