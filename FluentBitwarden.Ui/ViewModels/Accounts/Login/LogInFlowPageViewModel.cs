using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Infrastructure.Window;

namespace FluentBitwarden.ViewModels.Accounts.Login;

public sealed partial class LogInFlowPageViewModel : ObservableObject
{
    public LogInFlowPageViewModel(
        IAccountsClient accountsClient,
        IWindowManager windowManager,
        IAppCoordinator appCoordinator)
    {
        _windowManager = windowManager;
        _appCoordinator = appCoordinator;
        AccountsClient = accountsClient;
        CurrentStep = new LogInEmailStepViewModel(this, _windowManager);
    }

    private readonly IWindowManager _windowManager;
    private readonly IAppCoordinator _appCoordinator;

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

    internal Task OnSuccessLogIn(AccountProfile account) => _appCoordinator.RefreshSessionAsync();

    [RelayCommand]
    private void GoBack()
    {
        ShowEmailStep();
    }
}
