using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record AccountSignInSuccess(
    UserId UserId,
    string Email,
    AccountSessionTokens SessionTokens,
    AccountKeyMaterial AccountKeyMaterial,
    BitwardenEnvironment Environment);