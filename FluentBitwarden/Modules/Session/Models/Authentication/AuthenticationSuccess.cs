using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Session.Models.Authentication;

public sealed record AuthenticationSuccess(
    UserId UserId,
    string Email,
    SessionTokens SessionTokens,
    AccountCryptoMaterial AccountCryptoMaterial);