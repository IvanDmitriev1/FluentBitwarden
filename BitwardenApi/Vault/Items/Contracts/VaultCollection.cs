namespace BitwardenApi.Vault.Items.Contracts;

/// <summary>
/// Named VaultCollection to avoid ambiguity with BCL collection types at call sites.
/// </summary>
public sealed class VaultCollection
{
    public required CollectionId Id { get; init; }
    public required string Name { get; init; }
    public bool HidePasswords { get; init; }
    public bool ReadOnly { get; init; }
    public bool Manage { get; init; }
}
