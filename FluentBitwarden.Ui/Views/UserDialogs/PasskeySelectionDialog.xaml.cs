using FluentBitwarden.Infrastructure.UserDialogs;

namespace FluentBitwarden.Views.UserDialogs;

public sealed partial class PasskeySelectionDialog : ContentDialog, IUserDialog<Fido2Credential>
{
    public PasskeySelectionDialog(IReadOnlyList<Fido2Credential> credentials)
    {
        Credentials = credentials;
        InitializeComponent();
    }

    private Fido2Credential? _result;
    Fido2Credential IUserDialog<Fido2Credential>.Result => _result ?? throw new InvalidOperationException("Result is not set.");

    public IReadOnlyList<Fido2Credential> Credentials { get; }
    public bool HasItems => Credentials.Count > 0;
    public bool HasNoItems => !HasItems;

    private void CredentialList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CredentialList.SelectedItem is not Fido2Credential credential)
        {
            return;
        }

        SetResult(credential);
    }

    private void SetResult(Fido2Credential result)
    {
        _result = result;
        Hide();
    }
}
