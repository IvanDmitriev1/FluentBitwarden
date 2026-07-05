using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Contracts.Modules.Accounts.Login;

[MemoryPackable]
[MemoryPackUnion(0, typeof(Success))]
[MemoryPackUnion(1, typeof(TwoFactorRequired))]
[MemoryPackUnion(2, typeof(InvalidCredentials))]
[MemoryPackUnion(3, typeof(DeviceVerificationRequired))]
public abstract partial record AccountLoginOutcome
{
    private AccountLoginOutcome() { }

    [MemoryPackable]
    public sealed partial record Success(AccountProfile Account) : AccountLoginOutcome;

    [MemoryPackable]
    public sealed partial record TwoFactorRequired(
        IdentityTwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : AccountLoginOutcome;

    [MemoryPackable]
    public sealed partial record InvalidCredentials(string Message) : AccountLoginOutcome;

    [MemoryPackable]
    public sealed partial record DeviceVerificationRequired(string Message) : AccountLoginOutcome;
}
