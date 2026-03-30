using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Unlock;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface ICurrentSessionAccessor
{
    bool IsAuthenticated { get; }

    BitwardenClientContext CurrentContext { get; }
    UserId CurrentUser { get; }
    SessionTokens CurrentSession { get; }
    UserKeySession CurrentUserKeySession { get; }
}