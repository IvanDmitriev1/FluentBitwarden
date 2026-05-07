using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountUnlockRequest(AccountProfile Account)
{
    public sealed record MasterPasswordRequest(AccountProfile Account, string MasterPassword) : AccountUnlockRequest(Account);
    public sealed record WindowsHelloRequest(AccountProfile Account, IntPtr WindowHandle) : AccountUnlockRequest(Account);
}