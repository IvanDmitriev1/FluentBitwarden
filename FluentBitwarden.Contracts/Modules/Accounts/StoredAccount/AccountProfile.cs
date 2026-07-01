namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public sealed partial record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    DateTimeOffset LastSyncAt,
    AccountProfileDetails? Profile = null)
{
    [MemoryPackIgnore]
    public bool HasSyncedProfile => Profile is not null;
}