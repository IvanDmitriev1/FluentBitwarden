using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public abstract record AccountUnlockMethod
{
    public sealed record MasterPassword(string Password) : AccountUnlockMethod;

    public sealed record WindowsHello(NativeWindowHandle OwnerWindow) : AccountUnlockMethod;
}
