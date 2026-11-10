using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Input;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.Ui.Features.Vault.VaultCipherList;

[DependencyProperty<string>("SearchText")]
[DependencyProperty<Cipher>("SelectedCipher")]
[DependencyProperty<bool>("IsSearchFieldOpen")]
[DependencyProperty<VaultCipherQuery>("Query")]
[DependencyProperty<CipherId>("RequestedCipherId")]
public sealed partial class VaultCipherListView : UserControl
{
    public VaultCipherListView()
    {
        InitializeComponent();
    }

    public ObservableCollection<Cipher> FilteredCiphers { get; } = [];

    private void FocusSearch_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        VaultSearch.Focus(FocusState.Keyboard);
    }
}
