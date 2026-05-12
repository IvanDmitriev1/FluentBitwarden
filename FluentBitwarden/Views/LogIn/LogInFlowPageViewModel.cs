using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Views.Loading;
using FluentBitwarden.Views.LogIn.Models;
using FluentBitwarden.Views.LogIn.States;

namespace FluentBitwarden.Views.LogIn;

public sealed partial class LogInFlowPageViewModel : ObservableObject
{

    public LogInFlowPageViewModel(
        IAccountSessionManager accountSessionManager,
        INavigationService navigationService)
    {
        _navigationService = navigationService;
        AccountSessionManager = accountSessionManager;
        CurrentStep = new LogInEmailStepViewModel(this);
    }

    private readonly INavigationService _navigationService;

    internal IAccountSessionManager AccountSessionManager { get; }
    internal LogInFlowContext Context { get; } = new();


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial object CurrentStep { get; private set; }

    public bool CanGoBack => CurrentStep is not LogInEmailStepViewModel;

    internal void ShowEmailStep()
    {
        CurrentStep = new LogInEmailStepViewModel(this);
    }

    internal void ShowPasswordStep()
    {
        CurrentStep = new LogInPasswordStepViewModel(this);
    }

    internal void Show2FStep(AccountLoginnOutcome.TwoFactorRequired twoFactorRequired)
    {
        CurrentStep = new LogIn2FStepViewModel(twoFactorRequired, this);
    }

    internal void OnSuccessLogIn(AccountSignInSuccess success)
    {
        _navigationService.NavigateTo<LoadingPage>();
    }

    [RelayCommand]
    private void GoBack()
    {
        ShowEmailStep();
    }
}