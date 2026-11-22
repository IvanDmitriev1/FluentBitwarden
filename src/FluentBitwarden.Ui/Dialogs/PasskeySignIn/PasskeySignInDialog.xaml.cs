using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Ui.Dialogs.Abstractions;

namespace FluentBitwarden.Ui.Dialogs.PasskeySignIn;

public sealed partial class PasskeySignInDialog : ContentDialog,
    IContentDialog<PasskeySignInDialog, PasskeySelectCredentialRequest, PasskeySelectCredentialResponse>
{
    public static PasskeySignInDialog Create(PasskeySelectCredentialRequest request) => new(new PasskeySignInDialogViewModel(request));

    public PasskeySignInDialog(PasskeySignInDialogViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public PasskeySignInDialogViewModel ViewModel { get; }
    public PasskeySelectCredentialResponse Result => ViewModel.Result;
}
