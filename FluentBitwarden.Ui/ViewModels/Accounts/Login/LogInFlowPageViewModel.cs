using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Views.Startup;

namespace FluentBitwarden.ViewModels.Accounts.Login;

public sealed partial class LogInFlowPageViewModel : ObservableObject
{
    public LogInFlowPageViewModel(
        IAccountsClient accountsClient,
        INavigationService navigationService,
        IWindowManager windowManager)
    {
        _navigationService = navigationService;
        _windowManager = windowManager;
        AccountsClient = accountsClient;
        CurrentStep = new LogInEmailStepViewModel(this, _windowManager);
    }

    private readonly INavigationService _navigationService;
    private readonly IWindowManager _windowManager;

    internal IAccountsClient AccountsClient { get; }
    internal LogInFlowContext Context { get; } = new();


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial object CurrentStep { get; private set; }

    public bool CanGoBack => CurrentStep is not LogInEmailStepViewModel;

    internal void ShowEmailStep()
    {
        CurrentStep = new LogInEmailStepViewModel(this, _windowManager);
    }

    internal void ShowPasswordStep()
    {
        CurrentStep = new LogInPasswordStepViewModel(this);
    }

    internal void Show2FStep(AccountLoginOutcome.TwoFactorRequired twoFactorRequired)
    {
        CurrentStep = new LogIn2FStepViewModel(twoFactorRequired, this);
    }

    internal void OnSuccessLogIn()
    {
        _navigationService.NavigateTo<LoadingPage>();
    }

    [RelayCommand]
    private void GoBack()
    {
        ShowEmailStep();
    }
}
