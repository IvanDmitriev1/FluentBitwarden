namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public sealed partial record AccountProfileDetails(
    string Name,
    string Culture,
    DateTimeOffset CreationDate);