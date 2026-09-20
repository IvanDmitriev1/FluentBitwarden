namespace FluentBitwarden.Contracts.Integrations.BrowserExtension.Models;

[Flags]
public enum BrowserCredentialPart
{
    None = 0,
    Username = 1 << 0,
    Password = 1 << 1,
    Totp = 1 << 2
}
