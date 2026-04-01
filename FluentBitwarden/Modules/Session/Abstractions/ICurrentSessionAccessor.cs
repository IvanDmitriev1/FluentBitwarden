using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface ICurrentSessionAccessor
{
    bool IsAuthenticated { get; }

    BitwardenClientContext CurrentContext { get; }
    UserId CurrentUser { get; }
    SessionTokens CurrentSession { get; }
    UserKeySession CurrentUserKeySession { get; }
}