using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Models;

public sealed class CipherQuery
{
    public static readonly CipherQuery QueryAll = new()
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

    public CipherSortField SortField { get; init; } = CipherSortField.Name;
    public CipherSortDirection SortDirection { get; init; } = CipherSortDirection.Ascending;

}