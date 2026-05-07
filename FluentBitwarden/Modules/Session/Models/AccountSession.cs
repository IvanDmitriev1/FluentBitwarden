using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record AccountSession(
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