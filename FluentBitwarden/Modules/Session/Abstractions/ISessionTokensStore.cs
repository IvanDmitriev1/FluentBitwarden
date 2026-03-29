using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface ISessionTokensStore
{
    void StoreAsync(
        UserId userId,
        SessionTokens tokens,
        CancellationToken cancellationToken = default);

    SessionTokens? TryGetAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    void RemoveAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}