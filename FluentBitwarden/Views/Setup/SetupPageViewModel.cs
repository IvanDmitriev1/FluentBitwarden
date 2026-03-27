using FluentBitwarden.Views.Setup.States;

namespace FluentBitwarden.Views.Setup;

public partial class SetupPageViewModel : ObservableObject
{
    public SetupPageViewModel()
    {
        GoToEmail();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial object CurrentState { get; private set; } = null!;

    public bool CanGoBack => CurrentState is not EmailSignInStepState;

    private void GoToEmail()
    {
        CurrentState = new EmailSignInStepState();
    }

    private void GoToPasswordSignIn()
    {
        CurrentState = new PasswordSignInStepState();
    }

    private void GoTo2Fa()
    {

    }
}