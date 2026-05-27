using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Vault.Models;

public sealed class VaultCipherQuery
{
    public static readonly VaultCipherQuery QueryAll = new()
    {
        SearchText = string.Empty,
        CipherType = null,
    };

    public string SearchText { get; init; } = string.Empty;

    public CipherType? CipherType { get; init; }
    public FolderId FolderId { get; init; } = FolderId.Empty;
    public bool FavoritesOnly { get; init; }
    public bool IncludeDeleted { get; init; }
    public bool IncludeArchived { get; init; }
    public int? Limit { get; init; }

    public VaultCipherSortField SortField { get; init; } = VaultCipherSortField.Name;
    public VaultCipherSortDirection SortDirection { get; init; } = VaultCipherSortDirection.Ascending;
}