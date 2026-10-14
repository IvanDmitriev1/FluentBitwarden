namespace FluentBitwarden.Contracts.AppSession;

public sealed class UnlockedSessionRequiredException()
    : InvalidOperationException("The vault must be unlocked to perform this operation.");
