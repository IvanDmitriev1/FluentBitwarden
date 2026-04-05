using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface ICurrentSessionAccessor
{
    bool IsAuthenticated { get; }

    BitwardenClientContext CurrentContext { get; }
    UserId CurrentUser { get; }
    SessionTokens CurrentSession { get; }
    DecryptedUserKey CurrentDecryptedUserKey { get; }
}