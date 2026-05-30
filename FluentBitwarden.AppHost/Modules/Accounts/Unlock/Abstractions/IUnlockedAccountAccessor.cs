using BitwardenApi.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

internal interface IUnlockedAccountAccessor
{
    bool HasUnlockedAccount { get; }

    AccountProfile CurrentAccount { get; }
}
