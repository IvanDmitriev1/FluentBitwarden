using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountUnlockRequest(AccountProfile Account)
{
    public sealed record MasterPasswordRequest(AccountProfile Account, string MasterPassword) : AccountUnlockRequest(Account);
    public sealed record TpmCngRequest(AccountProfile Account) : AccountUnlockRequest(Account);
}