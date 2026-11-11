using BitwardenApi.Client;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Ui.Application.Abstractions;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Password;

public sealed partial class SetupPasswordPageViewModel(
    IAccountClient accountClient,
    ISetupNavigator navigator,
    IAppSession sessionRefresher) : ObservableObject, INavigationAware, INavigationAware<SetupPasswordParameters>
{
    private BitwardenClientContext? _context;
    private SetupEmailParameters? _emailState;
    private CancellationToken _frameCancellationToken;
    private CancellationTokenSource? _operationCancellation;

    [ObservableProperty]
    public partial string Email { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string ServerDisplayName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial int PasswordResetRequest { get; private set; }

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        _frameCancellationToken = cancellationToken;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedToAsync(SetupPasswordParameters parameters, CancellationToken cancellationToken)
    {
        _frameCancellationToken = cancellationToken;
        _context = parameters.Context;
        _emailState = parameters.EmailState;
        Email = parameters.Email;
        ServerDisplayName = parameters.ServerDisplayName;
        Error = null;
        PasswordResetRequest++;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        _context = null;
        _emailState = null;
        Email = string.Empty;
        ServerDisplayName = string.Empty;
        Error = null;
        IsBusy = false;
        PasswordResetRequest++;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (!IsBusy && _emailState is { } emailState)
            navigator.GoBackToEmail(emailState);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LoginAsync(string? password)
    {
        try
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(password))
            {
                Error = "Enter your master password.";
                return;
            }

            if (_context is not { } context)
                return;

            using var operation = CancellationTokenSource.CreateLinkedTokenSource(_frameCancellationToken);
            _operationCancellation = operation;
            Error = null;
            await AuthenticateAsync(new AccountAuthenticationRequest.Password(context, Email, password), operation.Token);
        }
        finally
        {
            PasswordResetRequest++;
        }
    }

    private async Task AuthenticateAsync(AccountAuthenticationRequest request, CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            AccountAuthenticationOutcome outcome = await accountClient.AuthenticateAsync(request, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            switch (outcome)
            {
                case AccountAuthenticationOutcome.Success:
                    await sessionRefresher.RefreshSessionAsync(cancellationToken);
                    break;
                case AccountAuthenticationOutcome.InvalidCredentials invalid:
                    Error = invalid.Message;
                    break;
                case AccountAuthenticationOutcome.DeviceVerificationRequired verification:
                    Error = verification.Message;
                    break;
                case AccountAuthenticationOutcome.TwoFactorRequired challenge when _context is { } context && _emailState is { } emailState:
                    navigator.ShowTwoFactor(new SetupTwoFactorParameters(context, challenge, emailState));
                    break;
                default:
                    Error = "This sign-in method returned an unsupported result.";
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!cancellationToken.IsCancellationRequested)
                Error = "Unable to connect. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
            _operationCancellation = null;
        }
    }
}
