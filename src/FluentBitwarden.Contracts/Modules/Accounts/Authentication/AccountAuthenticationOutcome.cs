using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Contracts.Modules.Accounts.Authentication;

[MemoryPackable]
[MemoryPackUnion(0, typeof(Success))]
[MemoryPackUnion(1, typeof(TwoFactorRequired))]
[MemoryPackUnion(2, typeof(InvalidCredentials))]
[MemoryPackUnion(3, typeof(DeviceVerificationRequired))]
public abstract partial record AccountAuthenticationOutcome
{
    private AccountAuthenticationOutcome() { }

    [MemoryPackable]
    public sealed partial record Success(AccountProfile Account) : AccountAuthenticationOutcome;

    [MemoryPackable]
    public sealed partial record TwoFactorRequired(
        IdentityTwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : AccountAuthenticationOutcome;

    [MemoryPackable]
    public sealed partial record InvalidCredentials(string Message) : AccountAuthenticationOutcome;

    [MemoryPackable]
    public sealed partial record DeviceVerificationRequired(string Message) : AccountAuthenticationOutcome;
}
