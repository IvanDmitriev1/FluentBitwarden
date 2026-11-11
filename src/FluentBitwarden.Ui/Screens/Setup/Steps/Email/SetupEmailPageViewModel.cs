using System.ComponentModel.DataAnnotations;
using BitwardenApi.Client;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Windows.Abstractions;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Email;

public sealed partial class SetupEmailPageViewModel(
    IAccountClient accountClient,
    IWindowManager windowManager,
    ISetupNavigator navigator,
    IAppSession sessionRefresher) : ObservableObject,
    INavigationAware,
    INavigationAware<SetupEmailParameters>
{
    private CancellationToken _frameCancellationToken;
    private CancellationTokenSource? _operationCancellation;

    public SetupServerOption[] Servers { get; } = [SetupServerOption.Us, SetupServerOption.Eu, SetupServerOption.Custom];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomServerSelected))]
    public partial SetupServerOption? SelectedServer { get; set; } = SetupServerOption.Us;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CustomServerUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? EmailError { get; set; }

    [ObservableProperty]
    public partial string? ServerError { get; set; }

    [ObservableProperty]
    public partial string? AuthenticationError { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    public bool IsCustomServerSelected => SelectedServer?.IsCustom == true;

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        _frameCancellationToken = cancellationToken;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedToAsync(SetupEmailParameters parameters, CancellationToken cancellationToken)
    {
        _frameCancellationToken = cancellationToken;
        Email = parameters.Email;
        SelectedServer = parameters.SelectedServer;
        CustomServerUrl = parameters.CustomServerUrl;
        EmailError = null;
        ServerError = null;
        AuthenticationError = null;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        IsBusy = false;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private void Continue()
    {
        Validate();
        if (EmailError is not null || ServerError is not null)
            return;

        var emailState = GetEmailState();
        navigator.ShowPassword(new SetupPasswordParameters(
            Email.Trim(),
            CreateContext(),
            SelectedServer!.Title,
            emailState));
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task AuthenticateWithPasskeyAsync()
    {
        if (IsBusy)
            return;

        AuthenticationError = null;
        Validate();
        if (EmailError is not null || ServerError is not null)
            return;

        await AuthenticateAsync(new AccountAuthenticationRequest.Passkey(
            CreateContext(),
            windowManager.NativeWindowHandle));
    }

    private async Task AuthenticateAsync(AccountAuthenticationRequest request)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_frameCancellationToken);
        _operationCancellation = operation;
        IsBusy = true;
        try
        {
            AccountAuthenticationOutcome outcome = await accountClient.AuthenticateAsync(request, operation.Token);
            if (operation.IsCancellationRequested)
                return;

            switch (outcome)
            {
                case AccountAuthenticationOutcome.Success:
                    await sessionRefresher.RefreshSessionAsync(operation.Token);
                    break;
                case AccountAuthenticationOutcome.TwoFactorRequired:
                    AuthenticationError = "This account requires two-step verification. Sign in with your master password instead.";
                    break;
                case AccountAuthenticationOutcome.DeviceVerificationRequired verification:
                    AuthenticationError = verification.Message;
                    break;
                case AccountAuthenticationOutcome.InvalidCredentials invalid:
                    AuthenticationError = invalid.Message;
                    break;
                default:
                    AuthenticationError = "This sign-in method returned an unsupported result.";
                    break;
            }
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!operation.IsCancellationRequested)
                AuthenticationError = "Unable to connect. Check your connection and try again.";
        }
        finally
        {
            if (ReferenceEquals(_operationCancellation, operation))
                _operationCancellation = null;
            IsBusy = false;
        }
    }

    private SetupEmailParameters GetEmailState() => new(Email.Trim(), SelectedServer, CustomServerUrl);

    private BitwardenClientContext CreateContext() => new(
        SelectedServer!.ToEnvironment(CustomServerUrl),
        DeviceIdentity.DeviceInfo);

    private void Validate()
    {
        EmailError = new EmailAddressAttribute().IsValid(Email.Trim())
            ? null
            : "Enter a valid email address.";

        ServerError = SelectedServer is null
            ? "Choose a server."
            : SelectedServer.IsCustom && !IsValidCustomServer(CustomServerUrl)
                ? GetCustomServerError(CustomServerUrl)
                : null;

        AuthenticationError = null;
    }

    private static bool IsValidCustomServer(string serverUrl) =>
        Uri.TryCreate(serverUrl.Trim(), UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(uri.Host) &&
        uri.UserInfo.Length == 0 &&
        uri.Query.Length == 0 &&
        uri.Fragment.Length == 0;

    private static string GetCustomServerError(string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            return "Enter your self-hosted server URL.";

        if (!Uri.TryCreate(serverUrl.Trim(), UriKind.Absolute, out var uri))
            return "Enter a valid server URL.";

        return uri.Scheme == Uri.UriSchemeHttps
            ? "Enter a valid server URL."
            : "The server URL must use HTTPS.";
    }
}
