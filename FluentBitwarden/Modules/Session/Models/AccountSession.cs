using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record AccountSession(
    UserId UserId,
    BitwardenClientContext Context,
    AccountSessionTokens AccountSessionTokens,
    DecryptedUserKey DecryptedUserKey,
    DateTime UnlockedAt) : IDisposable
{
    public void Dispose()
    {
        DecryptedUserKey.Dispose();
    }
}