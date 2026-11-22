using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentBitwarden.Ui.Dialogs.PasskeySignIn;

public sealed partial class PasskeySignInDialogViewModel
{
    private static readonly ResourceLoader Resources = new();

    public PasskeySignInDialogViewModel(PasskeySelectCredentialRequest request)
    {
        if (request.Candidates.Length != 1)
            throw new ArgumentException("The sign-in dialog requires exactly one passkey candidate.", nameof(request));

        var candidate = request.Candidates[0];
        string rpId = request.RpId;
        Candidate = new PasskeySignInCandidateViewModel(candidate, rpId);
        SiteTitle = string.Format(Resources.GetString("PasskeySignInTitleFormat"), rpId);
    }

    public PasskeySignInCandidateViewModel Candidate { get; }
    public string SiteTitle { get; }
    public PasskeySelectCredentialResponse Result { get; private set; }

    [RelayCommand]
    private void SignIn() => Result = new PasskeySelectCredentialResponse(Candidate.CipherId);
}
