namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public sealed partial record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    DateTimeOffset LastSyncAt);