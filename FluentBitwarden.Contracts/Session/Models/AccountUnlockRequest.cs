namespace FluentBitwarden.Contracts.Session.Models;

public abstract record AccountUnlockRequest(AccountProfile Account) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.Unlock;

    public sealed record MasterPasswordRequest(AccountProfile Account, string MasterPassword) : AccountUnlockRequest(Account);
    public sealed record WindowsHelloRequest(AccountProfile Account, IntPtr OwnerWindowHandle) : AccountUnlockRequest(Account);
}