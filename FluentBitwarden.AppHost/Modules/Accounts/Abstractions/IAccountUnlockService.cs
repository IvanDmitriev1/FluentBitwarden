using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Abstractions;

/// <summary>
/// Accounts' sibling-facing unlock surface: verify a credential and hand back the user key.
/// </summary>
internal interface IAccountUnlockService
{
    AccountUnlockResult Unlock(AccountUnlockRequest request);
}
