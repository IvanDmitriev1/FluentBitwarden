using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Storage;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class DpapiSessionTokensStore : ISessionTokensStore
{
    private static readonly byte[] Entropy = "bw_session_v1"u8.ToArray();

    public void Store(UserId userId, SessionTokens tokens)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(tokens, SessionJsonContext.Default.SessionTokens);
        var blob = ProtectedData.Protect(json, Entropy, DataProtectionScope.CurrentUser);

        var path = SessionPath(userId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, blob);

        CryptographicOperations.ZeroMemory(json);
    }

    public SessionTokens? TryGet(UserId userId)
    {
        var path = SessionPath(userId);
        if (!File.Exists(path))
            return null;

        var blob = File.ReadAllBytes(path);
        var json = ProtectedData.Unprotect(blob, Entropy, DataProtectionScope.CurrentUser);
        var result = JsonSerializer.Deserialize<SessionTokens>(json, SessionJsonContext.Default.SessionTokens);

        CryptographicOperations.ZeroMemory(json);
        return result;
    }

    public void Remove(UserId userId)
    {
        var path = SessionPath(userId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string SessionPath(UserId userId) =>
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "Sessions", $"{userId}.bin");
}
