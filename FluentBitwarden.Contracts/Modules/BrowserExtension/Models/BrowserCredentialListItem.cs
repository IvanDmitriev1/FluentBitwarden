namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialListItem(
    string Id,
    string? Username,
    string? DisplayName);
