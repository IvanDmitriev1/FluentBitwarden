using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;

namespace FluentBitwarden.ViewModels.SetUp;

public partial class SetupPageViewModel : ObservableObject
{
    public enum SetupStep
    {
        PasswordSignIn,
        TwoFactor,
    }

    private readonly INavigationService _navigationService;
    private readonly PasswordSignInStepViewModel _passwordStepViewModel;
    private readonly TwoFactorStepViewModel _twoFactorStepViewModel;
    private readonly PageOperationState _operationState = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentStepViewModel))]
    public partial SetupStep CurrentStep { get; set; } = SetupStep.PasswordSignIn;

    public bool HasError => _operationState.HasError;
    public string ErrorMessage => _operationState.ErrorMessage;
    public bool IsBusy => _operationState.IsBusy;

    public object? CurrentStepViewModel => CurrentStep switch
    {
        SetupStep.TwoFactor => _twoFactorStepViewModel,
        _ => _passwordStepViewModel,
    };

    public SetupPageViewModel(
        IAuthService authService,
        IEnvironmentConfig environmentConfig,
        INavigationService navigationService)
    {
        _navigationService = navigationService;
        _operationState.PropertyChanged += OnOperationStatePropertyChanged;

        _passwordStepViewModel = new PasswordSignInStepViewModel(this, authService, environmentConfig);
        _twoFactorStepViewModel = new TwoFactorStepViewModel(this, authService);
    }

    public void EnterTwoFactorStep(TwoFactorChallenge challenge)
    {
        _twoFactorStepViewModel.LoadChallenge(challenge);
        CurrentStep = SetupStep.TwoFactor;
    }

    public void ResetToPasswordStep()
    {
        _twoFactorStepViewModel.Reset();
        CurrentStep = SetupStep.PasswordSignIn;
    }

    public Task CompleteAuthenticatedSessionAsync()
    {
        _navigationService.Navigate<VaultPage>(clearBackStack: true);
        return Task.CompletedTask;
    }

    public Task RunBusyAsync(Func<Task> operation)
        => _operationState.RunBusyAsync(operation);

    public void ClearStatus()
        => _operationState.ClearStatus();

    public void ShowError(string message)
        => _operationState.ShowError(message);

    public void ShowError(Exception exception)
        => _operationState.ShowError(exception);

    private void OnOperationStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(e.PropertyName);
}
