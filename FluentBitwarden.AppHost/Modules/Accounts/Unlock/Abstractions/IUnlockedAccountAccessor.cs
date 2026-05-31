using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

internal interface IUnlockedAccountAccessor
{
    bool HasUnlockedAccount { get; }

    AccountProfile CurrentAccount { get; }
    DecryptedUserKey UserKey { get; }
}
