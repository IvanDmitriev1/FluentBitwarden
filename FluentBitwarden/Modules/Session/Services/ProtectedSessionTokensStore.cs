using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Internal;
using FluentBitwarden.Modules.Session.Models;
using System.Security.Cryptography;
using Windows.Storage;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class ProtectedSessionTokensStore(ISecretProtector secretProtector) : ISessionTokensStore
{
    public void Store(UserId userId, SessionTokens tokens)
    {
        using var payloadOwner = SessionTokensCodec.Serialize(tokens, out var bytesWritten);

        try
        {
            secretProtector.Protect(SessionPath(userId), payloadOwner.Span[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payloadOwner.Span);
        }
    }

    public SessionTokens? Get(UserId userId)
    {
        var payload = secretProtector.TryUnprotect(SessionPath(userId));
        if (payload is null)
        {
            return null;
        }

        try
        {
            return SessionTokensCodec.TryDeserialize(payload, out var tokens)
                ? tokens
                : null;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
        }
    }

    public void Remove(UserId userId)
    {
        TryDelete(SessionPath(userId));
    }

    private static string SessionPath(UserId userId) =>
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "Sessions", $"{userId}.session");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
