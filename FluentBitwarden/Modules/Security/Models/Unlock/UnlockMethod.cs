namespace FluentBitwarden.Modules.Security.Models.Unlock;

public enum UnlockMethod : byte
{
    MasterPassword = 0,
    Pin = 1,
    WindowsHello = 2
}
