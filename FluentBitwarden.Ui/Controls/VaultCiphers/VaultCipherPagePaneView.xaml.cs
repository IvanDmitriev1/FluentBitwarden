using System.Collections.ObjectModel;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<string>("SearchText")]
[DependencyProperty<VaultCipherType?>("SelectedCipherType")]
[DependencyProperty<VaultCipherSortField>("CipherSortField")]
[DependencyProperty<VaultCipherSortDirection>("CipherSortDirection")]
[DependencyProperty<VaultCipher>("SelectedCipher")]
[DependencyProperty<ObservableCollection<VaultCipher>>("FilteredCiphers")]
[DependencyProperty<bool>("IsSearchFieldOpen")]
public sealed partial class VaultCipherPagePaneView : UserControl
{
    public VaultCipherPagePaneView()
    {
        InitializeComponent();
    }

    partial void OnSearchTextChanged(string? oldValue, string? newValue)
    {
        if (!string.IsNullOrWhiteSpace(newValue))
            IsSearchFieldOpen = true;
    }
}
