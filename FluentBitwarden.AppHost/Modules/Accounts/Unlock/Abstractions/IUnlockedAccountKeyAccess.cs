using BitwardenApi.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

internal interface IUnlockedAccountKeyAccess
{
    DecryptedUserKey UserKey { get; }
}