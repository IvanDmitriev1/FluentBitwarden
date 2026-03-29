using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface ITokenRefreshService
{
    Task<SessionTokens> RefreshAsync(
        UserId userId,
        BitwardenClientContext context,
        SessionTokens current,
        CancellationToken ct = default);
}