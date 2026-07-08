namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

[MemoryPackable]
public sealed partial record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment)
{
    [MemoryPackIgnore]
    public BitwardenAccountContext BitwardenAccountContext { get; } = new(UserId, Environment);
}
