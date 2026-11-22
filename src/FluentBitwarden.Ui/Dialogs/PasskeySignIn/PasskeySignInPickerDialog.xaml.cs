using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Ui.Dialogs.Abstractions;

namespace FluentBitwarden.Ui.Dialogs.PasskeySignIn;

public sealed partial class PasskeySignInPickerDialog : ContentDialog,
    IContentDialog<PasskeySignInPickerDialog, PasskeySelectCredentialRequest, PasskeySelectCredentialResponse>
{
    public static PasskeySignInPickerDialog Create(PasskeySelectCredentialRequest request) => new(new PasskeySignInPickerDialogViewModel(request));

    public PasskeySignInPickerDialog(PasskeySignInPickerDialogViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public PasskeySignInPickerDialogViewModel ViewModel { get; }
    public PasskeySelectCredentialResponse Result => ViewModel.Result;
}
