namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial record BrowserVaultStatusResponse(
    bool IsAvailable,
    bool IsUnlocked);
