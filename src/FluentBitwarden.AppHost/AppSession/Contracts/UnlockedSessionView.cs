using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.AppSession.Contracts;

public readonly record struct UnlockedSessionView(
    AccountProfile Account,
    IUnlockedVault Vault,
    CancellationToken CancellationToken);
