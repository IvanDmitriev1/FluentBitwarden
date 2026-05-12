using BitwardenApi.Models;

namespace FluentBitwarden.Views.Passkey;

public sealed partial class PasskeySelectPageViewModel(Action<Fido2Credential> onCredentialSelection) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    public partial IReadOnlyList<Fido2Credential> Items { get; set; } = [];

    [ObservableProperty]
    public partial Fido2Credential? SelectedItem { get; set; }

    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => !HasItems;

    partial void OnSelectedItemChanged(Fido2Credential? value)
    {
        if (value is not null)
        {
            onCredentialSelection.Invoke(value);
        }
    }
}