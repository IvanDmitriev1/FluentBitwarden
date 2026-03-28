using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Session.Models.Authentication;

public sealed record PasswordSignInRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPassword);