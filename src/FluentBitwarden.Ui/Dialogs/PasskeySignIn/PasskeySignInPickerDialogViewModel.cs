using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentBitwarden.Ui.Dialogs.PasskeySignIn;

public sealed partial class PasskeySignInPickerDialogViewModel : ObservableObject
{
    private static readonly ResourceLoader Resources = new();

    public PasskeySignInPickerDialogViewModel(PasskeySelectCredentialRequest request)
    {
        Candidates = request.Candidates
            .Select(candidate => new PasskeySignInCandidateViewModel(candidate, request.RpId))
            .ToArray();

        Subtitle = string.Format(Resources.GetString("PasskeySignInPickerSubtitleFormat"), request.RpId);
        SelectedCandidate = Candidates.FirstOrDefault();
    }

    public PasskeySignInCandidateViewModel[] Candidates { get; }
    public string Subtitle { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSignIn))]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial PasskeySignInCandidateViewModel? SelectedCandidate { get; set; }

    public bool CanSignIn => SelectedCandidate is not null;
    public PasskeySelectCredentialResponse Result { get; private set; }

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private void SignIn()
    {
        if (SelectedCandidate is { } candidate)
            Result = new PasskeySelectCredentialResponse(candidate.CipherId);
    }
}
