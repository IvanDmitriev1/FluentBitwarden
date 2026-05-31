using FluentBitwarden.Contracts.Modules.Accounts.Unlock.General;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

internal interface IAccountUnlockService
{
    AccountUnlockOutcome Unlock(AccountUnlockRequest request);
    void Lock();
}