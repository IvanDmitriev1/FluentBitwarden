namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial record BrowserCredentialAvailabilityResponse(
    bool VaultLocked,
    int Count,
    IReadOnlyList<BrowserCredentialListItem> Items);
