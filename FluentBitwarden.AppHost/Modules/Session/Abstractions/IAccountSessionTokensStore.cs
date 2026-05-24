using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountSessionTokensStore
{
    void Store(UserId userId, RefreshToken token);
    RefreshToken Get(UserId userId);
    void Remove(UserId userId);
}