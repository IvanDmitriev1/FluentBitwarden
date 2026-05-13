using System.Linq;
using BitwardenApi.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Internal;

internal static class VaultSearchExtensions
{
    public static IEnumerable<VaultCipher> ApplySort(
        this IEnumerable<VaultCipher> source,
        CipherSortField sortField,
        CipherSortDirection sortDirection) => (sortField, sortDirection) switch
    {
        (CipherSortField.Name, CipherSortDirection.Ascending) =>
            source.OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase),

        (CipherSortField.Name, CipherSortDirection.Descending) =>
            source.OrderByDescending(static x => x.Name, StringComparer.CurrentCultureIgnoreCase),

        (CipherSortField.CreationDate, CipherSortDirection.Ascending) =>
            source.OrderBy(static x => x.CreationDate),

        (CipherSortField.CreationDate, CipherSortDirection.Descending) =>
            source.OrderByDescending(static x => x.CreationDate),

        (CipherSortField.RevisionDate, CipherSortDirection.Ascending) =>
            source.OrderBy(static x => x.RevisionDate),

        (CipherSortField.RevisionDate, CipherSortDirection.Descending) =>
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