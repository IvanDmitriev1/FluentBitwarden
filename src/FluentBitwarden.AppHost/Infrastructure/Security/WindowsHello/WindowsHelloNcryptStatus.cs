using System.Security.Cryptography;

namespace FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;

internal static class WindowsHelloNcryptStatus
{
    public const int NteBadData = unchecked((int)0x80090005);

    private const int ErrorSuccess = 0;
    private const int NteNoKey = unchecked((int)0x8009000D);
    private const int NteBadKeyset = unchecked((int)0x80090016);
    private const int NteUserCancelled = unchecked((int)0x80090036);
    private const int ErrorCancelled = unchecked((int)0x800704C7);

    /// <summary>
    /// Converts NCrypt status codes into the unlock exceptions handled by the account unlock flow.
    /// </summary>
    public static void ThrowIfFailed(int status, string operation, int? ignoredStatus = null)
    {
        if (status == ErrorSuccess || status == ignoredStatus)
            return;

        throw status switch
        {
            NteUserCancelled or ErrorCancelled => new WindowsHelloAuthenticationCanceledException(),
            NteNoKey or NteBadKeyset => new WindowsHelloKeyUnavailableException(),
            _ => new CryptographicException($"{operation} failed with security status 0x{unchecked((uint)status):X8}.")
        };
    }

    /// <summary>
    /// Returns whether an NCrypt call completed successfully.
    /// </summary>
    public static bool Succeeded(int status) => status == ErrorSuccess;
}
