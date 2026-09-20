using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Contracts.AppSession.Status;

[MemoryPackable]
public readonly partial record struct AppSessionSnapshot(
    AppSessionStatus Status,
    AccountProfile? CurrentAccount,
    DateTimeOffset Revision);
