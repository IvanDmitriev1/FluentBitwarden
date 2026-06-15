namespace FluentBitwarden.Contracts.Modules.Browser;

[MemoryPackable]
public sealed partial record BrowserCredentialFillResponse(
    string? Username,
    string? Password);
