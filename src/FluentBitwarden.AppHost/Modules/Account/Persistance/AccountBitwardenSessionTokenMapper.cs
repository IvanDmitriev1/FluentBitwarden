using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal static class AccountBitwardenSessionTokenMapper
{
    private static readonly byte[] Entropy = [.. "fbw_session_v1"u8];

    public static StoreParameters ToStoreParameters(UserId userId, SessionRefreshToken token) => new(
        userId.ToString(),
        Protect(token));

    public static UserIdParameters ToUserIdParameters(UserId userId) => new(userId.ToString());

    public static SessionRefreshToken ToDomain(Row? row)
    {
        byte[]? protectedBytes = row?.ProtectedRefreshToken;
        if (protectedBytes is null)
            return SessionRefreshToken.Empty;

        byte[] plaintext = [];

        try
        {
            plaintext = ProtectedData.Unprotect(
                encryptedData: protectedBytes,
                optionalEntropy: Entropy,
                scope: DataProtectionScope.CurrentUser);

            string tokenValue = Encoding.UTF8.GetString(plaintext);
            if (string.IsNullOrWhiteSpace(tokenValue))
                return SessionRefreshToken.Empty;

            return SessionRefreshToken.Parse(tokenValue, CultureInfo.InvariantCulture);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static byte[] Protect(SessionRefreshToken token)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(token.ToString());

        try
        {
            return ProtectedData.Protect(
                userData: plaintext,
                optionalEntropy: Entropy,
                scope: DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    internal sealed class Row
    {
        public byte[] ProtectedRefreshToken { get; set; } = [];
    }

    internal sealed record StoreParameters(string UserId, byte[] ProtectedRefreshToken);

    internal sealed record UserIdParameters(string UserId);
}
