using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;
using Path = System.IO.Path;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class AccountSessionTokensStore : IAccountSessionTokensStore
{
    private static readonly string SessionsDirectoryPath =
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "Sessions");

    private static byte[] Entropy => "fbw_session_v1"u8.ToArray();

    public AccountSessionTokensStore()
    {
        Directory.CreateDirectory(SessionsDirectoryPath);
    }

    public void Store(UserId userId, RefreshToken token)
    {
        string filePath = CreateSessionPath(userId);
        byte[] plaintext = Encoding.UTF8.GetBytes(token.ToString());

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(
                userData: plaintext,
                optionalEntropy: Entropy,
                scope: DataProtectionScope.CurrentUser);

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            fileStream.Write(protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public RefreshToken Get(UserId userId)
    {
        string filePath = CreateSessionPath(userId);

        if (!File.Exists(filePath))
            return RefreshToken.Empty;

        byte[] protectedBytes = File.ReadAllBytes(filePath);
        byte[] plaintext = [];

        try
        {
            plaintext = ProtectedData.Unprotect(
                encryptedData: protectedBytes,
                optionalEntropy: Entropy,
                scope: DataProtectionScope.CurrentUser);

            string tokenValue = Encoding.UTF8.GetString(plaintext);
            if (string.IsNullOrWhiteSpace(tokenValue))
                return RefreshToken.Empty;

            return RefreshToken.Parse(tokenValue);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public void Remove(UserId userId)
    {
        string filePath = CreateSessionPath(userId);

        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception e)
        {
            //
        }
    }

    private static string CreateSessionPath(UserId userId) =>
        Path.Combine(SessionsDirectoryPath, $"{userId}.session");
}