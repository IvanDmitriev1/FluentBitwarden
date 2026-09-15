using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Infrastructure.UserDialogs.Abstractions;

namespace FluentBitwarden.Views.UserDialogs;

public sealed partial class PasskeySelectionDialog : ContentDialog, IUserDialog<Fido2Credential>
{
    public PasskeySelectionDialog(IReadOnlyList<Fido2Credential> credentials)
    {
        InitializeComponent();

        Credentials = credentials;
    }

    private Fido2Credential? _result;

    public IReadOnlyList<Fido2Credential> Credentials { get; }
    public bool HasItems => Credentials.Count > 0;
    public bool HasNoItems => !HasItems;

    public bool TryGetResult([MaybeNullWhen(false)] out Fido2Credential result)
    {
        result = _result;
        return result is not null;
    }

    private void CredentialList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CredentialList.SelectedItem is not Fido2Credential credential)
            return;

        SetResult(credential);
    }

    private void SetResult(Fido2Credential result)
    {
        _result = result;
        Hide();
    }
}
