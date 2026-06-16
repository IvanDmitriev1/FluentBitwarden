namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityResponse(
    bool VaultLocked,
    int Count,
    IReadOnlyList<BrowserCredentialListItem> Items);
