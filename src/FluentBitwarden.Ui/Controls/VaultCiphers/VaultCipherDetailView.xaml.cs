using MemoryPack;
using System.Collections.ObjectModel;

namespace FluentBitwarden.Controls.VaultCiphers;

[DependencyProperty<Cipher>("SelectedCipher")]
[DependencyProperty<bool>("IsEditing")]
[DependencyProperty<ObservableCollection<Folder>>("Folders")]
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

    private static Cipher Clone(Cipher cipher) =>
        MemoryPackSerializer.Deserialize<Cipher>(MemoryPackSerializer.Serialize(cipher))!;
}