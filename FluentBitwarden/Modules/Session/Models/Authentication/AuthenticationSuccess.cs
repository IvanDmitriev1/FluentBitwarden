using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models.Authentication;

public sealed record AuthenticationSuccess(
    UserId UserId,
    string Email,
    SessionTokens SessionTokens,
    AccountDecryption AccountDecryption);