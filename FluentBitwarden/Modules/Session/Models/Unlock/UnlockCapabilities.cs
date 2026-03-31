namespace FluentBitwarden.Modules.Session.Models.Unlock;

public readonly record struct UnlockCapabilities(
    bool SupportsPin,
    bool SupportsWindowsHello,
    int RemainingPinAttempts);