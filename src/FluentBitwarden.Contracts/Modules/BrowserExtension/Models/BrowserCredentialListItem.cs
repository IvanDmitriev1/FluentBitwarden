namespace FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

[MemoryPackable]
public sealed partial record BrowserCredentialListItem(
    [property: StronglyTypedIdFormatter<CipherId>]
    CipherId Id,
    string Username);
