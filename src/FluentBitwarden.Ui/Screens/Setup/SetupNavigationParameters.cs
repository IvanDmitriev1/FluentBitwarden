using BitwardenApi.Client;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;

namespace FluentBitwarden.Ui.Screens.Setup;

public sealed record SetupEmailParameters(
    string Email,
    SetupServerOption? SelectedServer,
    string CustomServerUrl);

public sealed record SetupPasswordParameters(
    string Email,
    BitwardenClientContext Context,
    string ServerDisplayName,
    SetupEmailParameters EmailState);

public sealed record SetupTwoFactorParameters(
    BitwardenClientContext Context,
    AccountAuthenticationOutcome.TwoFactorRequired Challenge,
    SetupEmailParameters EmailState);
