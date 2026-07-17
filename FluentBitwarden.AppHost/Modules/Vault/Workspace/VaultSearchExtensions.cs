using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

internal static class VaultSearchExtensions
{
    public static VaultCipher[] FilterCiphers(this LoadedVaultData data, VaultCipherQuery query)
    {
        IEnumerable<VaultCipher> result = data.CiphersById.Values;

        if (query.FavoritesOnly)
            result = result.Where(static x => x.Favorite);

        if (!query.IncludeDeleted)
            result = result.Where(static x => x.DeletedDate is null);

        if (!query.FolderId.IsEmpty)
            result = result.Where(x => x.FolderId == query.FolderId);

        if (!query.CollectionId.IsEmpty)
        {
            if (!data.CipherIdsByCollectionId.TryGetValue(query.CollectionId, out var cipherIds))
                return [];

            result = result.Where(x => cipherIds.Contains(x.Id));
        }

        if (query.CipherType is not null)
            result = result.Where(x => x.Type == query.CipherType.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
            result = result.Where(x => x.MatchesSearchText(query.SearchText));

        result = result.ApplySort(query.SortField, query.SortDirection);

        if (query.Limit is not null)
            result = result.Take(query.Limit.Value);

        return result.ToArray();
    }

    public static IEnumerable<VaultCipher> ApplySort(
        this IEnumerable<VaultCipher> source,
        VaultCipherSortField sortField,
        VaultCipherSortDirection sortDirection) => (sortField, sortDirection) switch
        {
            (VaultCipherSortField.Name, VaultCipherSortDirection.Ascending) =>
                source.OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase),

            (VaultCipherSortField.Name, VaultCipherSortDirection.Descending) =>
                source.OrderByDescending(static x => x.Name, StringComparer.CurrentCultureIgnoreCase),

            (VaultCipherSortField.CreationDate, VaultCipherSortDirection.Ascending) =>
                source.OrderBy(static x => x.CreationDate),

            (VaultCipherSortField.CreationDate, VaultCipherSortDirection.Descending) =>
                source.OrderByDescending(static x => x.CreationDate),

            (VaultCipherSortField.RevisionDate, VaultCipherSortDirection.Ascending) =>
                source.OrderBy(static x => x.RevisionDate),

            (VaultCipherSortField.RevisionDate, VaultCipherSortDirection.Descending) =>
                source.OrderByDescending(static x => x.RevisionDate),

            _ =>
                source.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
        };

    public static bool MatchesSearchText(this VaultCipher cipher, string? searchText)
    {
        searchText = searchText?.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        return cipher.Name.ContainsSearchText(searchText) || cipher switch
        {
            LoginVaultCipher login => login.Username.ContainsSearchText(searchText),


            CardVaultCipher card => card.CardholderName.ContainsSearchText(searchText)
                                    || card.Brand.ContainsSearchText(searchText),

            IdentityVaultCipher identity => identity.Title.ContainsSearchText(searchText)
                                            || identity.FirstName.ContainsSearchText(searchText)
                                            || identity.MiddleName.ContainsSearchText(searchText)
                                            || identity.LastName.ContainsSearchText(searchText)
                                            || identity.Company.ContainsSearchText(searchText)
                                            || identity.Email.ContainsSearchText(searchText)
                                            || identity.Phone.ContainsSearchText(searchText)
                                            || identity.Username.ContainsSearchText(searchText)
                                            || identity.City.ContainsSearchText(searchText)
                                            || identity.State.ContainsSearchText(searchText)
                                            || identity.Country.ContainsSearchText(searchText)
                                            || identity.PostalCode.ContainsSearchText(searchText),

            _ => false
        };
    }

    private static bool ContainsSearchText(this string? value, string searchText)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }
}