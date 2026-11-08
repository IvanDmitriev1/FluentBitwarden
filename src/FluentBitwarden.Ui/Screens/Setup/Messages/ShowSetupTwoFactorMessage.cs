using BitwardenApi.Client;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;

namespace FluentBitwarden.Ui.Screens.Setup.Messages;

public sealed record ShowSetupTwoFactorMessage(
    BitwardenClientContext Context,
    AccountAuthenticationOutcome.TwoFactorRequired Challenge);
