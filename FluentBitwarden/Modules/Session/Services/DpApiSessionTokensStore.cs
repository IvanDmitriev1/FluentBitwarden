using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Internal;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Shared.Extensions;
using System.Security.Cryptography;
using Windows.Storage;


namespace FluentBitwarden.Modules.Session.Services;

internal sealed class DpApiSessionTokensStore : ISessionTokensStore
{
    private static readonly string SessionsDirectoryPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Sessions");
    private static byte[] Entropy => "bw_session_v1"u8.ToArray();

    private static string SessionPath(UserId userId) =>
        Path.Combine(SessionsDirectoryPath, $"{userId}.session");

    public void Store(UserId userId, SessionTokens tokens)
    {
        using var payloadOwner = SessionTokensCodec.Serialize(tokens, out var bytesWritten);
        var secret = payloadOwner.Span[..bytesWritten];

        try
        {
            using var protectedPayloadOwner = SpanOwner<byte>.Allocate(2400);

            if (!ProtectedData.TryProtect(
                    secret,
                    DataProtectionScope.CurrentUser,
                    protectedPayloadOwner.Span,
                    out bytesWritten,
                    Entropy))
            {
                throw new CryptographicException("DPAPI protection failed.");
            }

            var filePath = SessionPath(userId);
            Directory.CreateDirectory(SessionsDirectoryPath);

            var protectedPayload = protectedPayloadOwner.Span[..bytesWritten];
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 512);
            stream.Write(protectedPayload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payloadOwner.Span);
        }
    }

    public SessionTokens? Get(UserId userId)
    {
        var filePath = SessionPath(userId);

        if (!File.Exists(filePath))
            return null;

        using var protectedPayloadOwner = FilePathHelpers.ReadAllBytesOwner(filePath);
        using var plaintextOwner = SpanOwner<byte>.Allocate(protectedPayloadOwner.Length);

        try
        {
            if (!ProtectedData.TryUnprotect(
                    protectedPayloadOwner.Span,
                    DataProtectionScope.CurrentUser,
                    plaintextOwner.Span,
                    out var bytesWritten,
                    Entropy))
            {
                return null;
            }

            var payload = plaintextOwner.Span[..bytesWritten];

            return SessionTokensCodec.TryDeserialize(payload, out var tokens)
                ? tokens
                : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextOwner.Span);
        }
    }

    public void Remove(UserId userId)
    {
        try
        {
            var path = SessionPath(userId);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }
}
