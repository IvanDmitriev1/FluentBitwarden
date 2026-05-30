using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

internal interface IAccountUnlockService
{
    AccountUnlockOutcome Unlock(AccountUnlockRequest request);
    void Lock();
}