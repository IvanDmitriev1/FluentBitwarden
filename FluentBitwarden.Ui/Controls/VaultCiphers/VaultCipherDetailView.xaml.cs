using MemoryPack;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<VaultCipher>("SelectedCipher")]
[DependencyProperty<bool>("IsEditing")]
[DependencyProperty<ObservableCollection<VaultFolder>>("Folders")]
public sealed partial class VaultCipherDetailView : UserControl
{
    public VaultCipherDetailView()
    {
        InitializeComponent();
    }

    partial void OnIsEditingChanged()
    {
        if (SelectedCipher is null)
            return;

        if (IsEditing)
        {
            SelectedCipher = Clone(SelectedCipher);
        }
    }

    private static VaultCipher Clone(VaultCipher cipher) =>
        MemoryPackSerializer.Deserialize<VaultCipher>(MemoryPackSerializer.Serialize(cipher))!;
}