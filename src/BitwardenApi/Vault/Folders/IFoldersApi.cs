using BitwardenApi.Client;
using BitwardenApi.Vault.Folders.Responses;
using BitwardenApi.Vault.Folders.Requests;

namespace BitwardenApi.Vault.Folders;

public interface IFoldersApi
{
    Task<IReadOnlyList<FolderResponse>> GetFoldersAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default);

    Task<FolderResponse> GetFolderAsync(
        BitwardenAccountContext accountContext,
        FolderId folderId,
        CancellationToken cancellationToken = default);

    Task<FolderResponse> CreateFolderAsync(BitwardenAccountContext accountContext, FolderRequest request, CancellationToken cancellationToken = default);

    Task<FolderResponse> UpdateFolderAsync(BitwardenAccountContext accountContext, FolderId folderId, FolderRequest request, CancellationToken cancellationToken = default);

    Task DeleteFoldersAsync(BitwardenAccountContext accountContext, IReadOnlyList<FolderId> folderIds, CancellationToken cancellationToken = default);

    Task DeleteAllFoldersAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default);
}
