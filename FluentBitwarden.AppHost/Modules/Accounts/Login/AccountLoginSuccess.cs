using BitwardenApi.Models;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.Login;

public sealed record AccountLoginSuccess(
    UserId UserId,
    string Email,
    AccountAuthenticationTokens AuthenticationTokens,
    AccountKeyMaterial AccountKeyMaterial,
    BitwardenEnvironment Environment);
