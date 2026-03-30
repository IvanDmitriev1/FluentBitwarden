namespace FluentBitwarden.Modules.Session.Models.Unlock;

public sealed record UnlockCapabilities(
    bool SupportsMasterPassword,
    bool SupportsPin,
    bool SupportsWindowsHello,
    int RemainingPinAttempts);