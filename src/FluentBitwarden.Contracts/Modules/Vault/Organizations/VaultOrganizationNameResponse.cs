namespace FluentBitwarden.Contracts.Modules.Vault.Organizations;

[MemoryPackable]
public readonly partial record struct VaultOrganizationNameResponse(string? Name);
