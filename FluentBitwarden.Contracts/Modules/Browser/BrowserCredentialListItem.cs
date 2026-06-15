namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial record BrowserCredentialListItem(
    string Id,
    string? Username,
    string? DisplayName);
