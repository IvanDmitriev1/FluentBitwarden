namespace BitwardenApi.Vault.Collections.Models;

/// <summary>
/// Named Collection to avoid ambiguity with BCL collection types at call sites.
/// </summary>
public sealed class Collection
{
    public required CollectionId Id { get; init; }
    public required string Name { get; init; }
    public bool HidePasswords { get; init; }
    public bool ReadOnly { get; init; }
    public bool Manage { get; init; }
}
