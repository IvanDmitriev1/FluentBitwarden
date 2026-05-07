namespace FluentBitwarden.Modules.Session.Models;

[Flags]
public enum UnlockMethodType
{
    MasterPassword = 1,
    WindowsHello = 2
}