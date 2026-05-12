namespace FluentBitwarden.Modules.Session.Models;

[Flags]
public enum UnlockMethodType : byte
{
    MasterPassword = 1,
    WindowsHello = 2
}
