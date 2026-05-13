using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Models;

public sealed class CipherQuery
{
    public required string SearchText { get; init; }
    public int Limit { get; init; } = 500;

    public CipherType? CipherType { get; init; }
    public FolderId FolderId { get; init; } = FolderId.Empty;
    public bool FavoritesOnly { get; init; }
    public bool IncludeDeleted { get; init; }
    public bool IncludeArchived { get; init; }
}