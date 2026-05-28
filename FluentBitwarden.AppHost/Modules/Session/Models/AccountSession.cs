using BitwardenApi.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Modules.Session.Models;

internal sealed record AccountSession(
    AccountProfile Profile,
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
