using FluentBitwarden.AppHost.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Login;

internal sealed record AuthenticatedAccount(
    UserId UserId,
    string Email,
    AccountTokens AuthenticationTokens,
    AccountKeyMaterial AccountKeyMaterial,
    BitwardenEnvironment Environment);
