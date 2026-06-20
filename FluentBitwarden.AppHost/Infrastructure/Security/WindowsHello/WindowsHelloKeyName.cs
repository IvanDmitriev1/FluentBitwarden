using System.Security.Cryptography;
using System.Security.Principal;

namespace FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;

internal static class WindowsHelloKeyName
{
    private const string AppKeyPath = "FluentBitwarden/AccountUnlock";

    /// <summary>
    /// Builds a Passport key name scoped to the current Windows user SID and the account identifier.
    /// </summary>
    public static string Create(string accountKeyName)
    {
        string? sid = WindowsIdentity.GetCurrent().User?.Value;
        if (string.IsNullOrWhiteSpace(sid))
            throw new CryptographicException("The current Windows user SID could not be resolved.");

        return $"{sid}//{AppKeyPath}/{accountKeyName}";
    }
}
