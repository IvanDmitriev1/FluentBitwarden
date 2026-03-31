namespace FluentBitwarden.Modules.Session.Models.Unlock;

public enum UnlockMethod : byte
{
    MasterPassword = 0,
    Pin = 1,
    WindowsHello = 2
}
