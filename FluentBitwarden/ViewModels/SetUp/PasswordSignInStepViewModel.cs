using BitwaredApi;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentBitwarden.ViewModels.SetUp;

public sealed record SetupEnvironmentOption(string Title, string Subtitle, BitwardenEnvironment Environment);

public partial class PasswordSignInStepViewModel : ObservableObject
{
    public PasswordSignInStepViewModel(SetupPageViewModel parentViewModel)
    {
        ParentViewModel = parentViewModel;

        Environments =
        [
            new SetupEnvironmentOption("Bitwarden US", "bitwarden.com", BitwardenEnvironment.UnitedStates),
            new SetupEnvironmentOption("Bitwarden EU", "bitwarden.eu", BitwardenEnvironment.Europe),
        ];

        SelectedEnvironment = Environments[0];
    }

    public SetupPageViewModel ParentViewModel { get; }

    [ObservableProperty]
    public partial SetupEnvironmentOption? SelectedEnvironment { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPassword { get; set; } = string.Empty;

    public string ServerHintText { get; } = "Select your environment and sign in.";

    public SetupEnvironmentOption[] Environments { get; }
}
