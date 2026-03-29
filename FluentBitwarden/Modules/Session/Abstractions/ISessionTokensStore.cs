using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface ISessionTokensStore
{
    void Store(UserId userId, SessionTokens tokens);
    SessionTokens? TryGet(UserId userId);
    void Remove(UserId userId);
}