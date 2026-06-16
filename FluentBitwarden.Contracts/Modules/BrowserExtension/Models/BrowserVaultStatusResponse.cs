namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserVaultStatusResponse(
    bool IsAvailable,
    bool IsUnlocked);
