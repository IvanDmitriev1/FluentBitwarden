using BitwardenApi.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

internal interface IUnlockedAccountKeyAccess
{
    TResult UseDecryptedKey<TResult>(Func<AccountProfile, DecryptedUserKey, TResult> operation);
}