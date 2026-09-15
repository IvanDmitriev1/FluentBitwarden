namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

/// <summary>
/// Builds the collection-to-cipher membership index. Pure: no DB, no crypto.
/// </summary>
internal static class CipherCollectionIndex
{
    public static void Add(
        Dictionary<CollectionId, HashSet<CipherId>> index,
        CipherId cipherId,
        ReadOnlySpan<CollectionId> collectionIds)
    {
        if (collectionIds.Length == 0)
            return;

        foreach (var collectionId in collectionIds)
        {
            if (collectionId.IsEmpty)
                continue;

            if (!index.TryGetValue(collectionId, out var cipherIds))
            {
                cipherIds = [];
                index.Add(collectionId, cipherIds);
            }

            cipherIds.Add(cipherId);
        }
    }
}
