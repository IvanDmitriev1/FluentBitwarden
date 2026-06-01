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
    public sealed partial record Success() : AccountLoginOutcome;

    [MemoryPackable]
    public sealed partial record TwoFactorRequired(
        TwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : AccountLoginOutcome;

    [MemoryPackable]
    public sealed partial record InvalidCredentials(string Message) : AccountLoginOutcome;

    [MemoryPackable]
    public sealed partial record DeviceVerificationRequired(string Message) : AccountLoginOutcome;
}
