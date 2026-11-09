using System.ComponentModel.DataAnnotations;
using BitwardenApi.Client;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Platform.Infrastructure;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using FluentBitwarden.Ui.Screens.Setup.Messages;
using WinUIEx;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Email;

public sealed partial class SetupEmailViewModel(
    IAccountClient accountClient,
    IWindowManager windowManager,
    IMessenger messenger)
    : ObservableRecipient(messenger), IRecipient<ClearSetupEmailMessage>
{
    public void Receive(ClearSetupEmailMessage message) => Clear();

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

    [RelayCommand]
    private void Continue()
    {
        Validate();
        if (EmailError is not null || ServerError is not null)
            return;

        Messenger.Send(new ShowSetupPasswordMessage(Email.Trim(), CreateContext(), SelectedServer!.Title));
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

        var request = new AccountAuthenticationRequest.Passkey(
            CreateContext(),
            new NativeWindowHandle(windowManager.ActiveWindow.GetWindowHandle().ToInt64()));
        await AuthenticateAsync(request);
    }

    private async Task AuthenticateAsync(AccountAuthenticationRequest request)
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            Messenger.Send(new SetupAuthenticationStateChangedMessage(true));
            AccountAuthenticationOutcome outcome = await accountClient.AuthenticateAsync(request);
            HandleOutcome(outcome);
        }
        catch (Exception)
        {
            AuthenticationError = "Unable to connect. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
            Messenger.Send(new SetupAuthenticationStateChangedMessage(false));
        }
    }

    private void HandleOutcome(AccountAuthenticationOutcome outcome)
    {
        switch (outcome)
        {
            case AccountAuthenticationOutcome.Success:
                Messenger.Send(new AppRootSessionRefreshRequest());
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

    private void Clear()
    {
        Email = string.Empty;
        CustomServerUrl = string.Empty;
        EmailError = null;
        ServerError = null;
        AuthenticationError = null;
    }
}
