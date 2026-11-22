namespace FluentBitwarden.Contracts.Modules.Passkey.Models;

[MemoryPackable]
public sealed partial record PasskeySelectionCandidate(
    [property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId,
    string ItemName,
    string? AccountName,
    bool IsPersonalVault,
    string? OrganizationName);
