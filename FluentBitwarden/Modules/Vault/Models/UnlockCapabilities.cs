namespace FluentBitwarden.Modules.Vault.Models;

public sealed record UnlockCapabilities(
    bool SupportsMasterPassword,
    bool SupportsPin,
    bool SupportsWindowsHello,
    bool RequiresOnlineReauthentication,
    int RemainingPinAttempts);