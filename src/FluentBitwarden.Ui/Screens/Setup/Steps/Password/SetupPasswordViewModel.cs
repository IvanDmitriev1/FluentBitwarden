using BitwardenApi.Client;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Ui.Screens.Setup.Messages;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Password;

public sealed partial class SetupPasswordViewModel(IAccountClient accountClient, IMessenger messenger)
    : ObservableRecipient(messenger), IRecipient<ShowSetupPasswordMessage>
{
    private BitwardenClientContext _context;

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

    public void Receive(ShowSetupPasswordMessage message)
    {
        _context = message.Context;
        Email = message.Email;
        ServerDisplayName = message.ServerDisplayName;
        Error = null;
        PasswordResetRequest++;
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

            Error = null;
            await AuthenticateAsync(new AccountAuthenticationRequest.Password(_context, Email, password));
        }
        finally
        {
            PasswordResetRequest++;
        }
    }

    private async Task AuthenticateAsync(AccountAuthenticationRequest request)
    {
        IsBusy = true;

        try
        {
            Messenger.Send(new SetupAuthenticationStateChangedMessage(true));
            AccountAuthenticationOutcome outcome = await accountClient.AuthenticateAsync(request);
            HandleOutcome(outcome);
        }
        catch (Exception)
        {
            Error = "Unable to connect. Check your connection and try again.";
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
            case AccountAuthenticationOutcome.InvalidCredentials invalid:
                Error = invalid.Message;
                break;
            case AccountAuthenticationOutcome.DeviceVerificationRequired verification:
                Error = verification.Message;
                break;
            case AccountAuthenticationOutcome.TwoFactorRequired challenge:
                Messenger.Send(new ShowSetupTwoFactorMessage(_context, challenge));
                break;
            default:
                Error = "This sign-in method returned an unsupported result.";
                break;
        }
    }

}
