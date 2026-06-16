using BitwardenApi.Common.MemoryPackFormatters;

namespace FluentBitwarden.Contracts.Modules.Vault.Workspace;

[MemoryPackable]
public sealed partial class VaultCipherQuery : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.SearchCiphers;


    public static readonly VaultCipherQuery QueryAll = new()
    {
        SearchText = string.Empty,
        CipherType = null,
    };

    public string SearchText { get; init; } = string.Empty;

    public CipherType? CipherType { get; init; }

    [StronglyTypedIdFormatter<FolderId>]
    public FolderId FolderId { get; init; } = FolderId.Empty;

    [StronglyTypedIdFormatter<CollectionId>]
    public CollectionId CollectionId { get; init; } = CollectionId.Empty;

    public bool FavoritesOnly { get; init; }
    public bool IncludeDeleted { get; init; }
    public bool IncludeArchived { get; init; }
    public int? Limit { get; init; }

    public VaultCipherSortField SortField { get; init; } = VaultCipherSortField.Name;
    public VaultCipherSortDirection SortDirection { get; init; } = VaultCipherSortDirection.Ascending;
}
