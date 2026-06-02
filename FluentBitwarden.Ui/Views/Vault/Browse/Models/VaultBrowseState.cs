using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Models;
using FluentBitwarden.Contracts.Infrastructure.Settings.Models;
using Windows.Storage;

namespace FluentBitwarden.Views.Vault.Browse.Models;

public sealed record VaultBrowseState(string SearchText, CipherId SelectedCipherId) : ICompositeSettingValue<VaultBrowseState>
{
    public static VaultBrowseState Default => new(string.Empty, CipherId.Empty);

    public static void Write(ApplicationDataCompositeValue composite, VaultBrowseState value)
    {
        composite[nameof(SearchText)] = value.SearchText;

        if (value.SelectedCipherId == CipherId.Empty)
            composite.Remove(nameof(SelectedCipherId));
        else
            composite[nameof(SelectedCipherId)] = value.SelectedCipherId.ToString();
    }

    public static bool TryRead(ApplicationDataCompositeValue composite, [NotNullWhen(true)] out VaultBrowseState? value)
    {
        if (!composite.TryReadString(nameof(SearchText), out var searchText) ||
            !composite.TryReadString(nameof(SelectedCipherId), out var selectedCipherId) ||
            !CipherId.TryParse(selectedCipherId, null, out var cipherId))
        {
            value = null;
            return false;
        }

        value = new VaultBrowseState(searchText, cipherId);
        return true;
    }
}