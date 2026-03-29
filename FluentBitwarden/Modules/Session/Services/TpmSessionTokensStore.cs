using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Storage;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class TpmSessionTokensStore : ISessionTokensStore
{
    private const string TpmProviderName = "Microsoft Platform Crypto Provider";
    private const string TpmKeyName = "bw_session_key_v1";
    private const int RsaKeySizeBits = 2048;
    private const int SessionKeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static bool IsSupported()
    {
        try
        {
            using var rsa = OpenRsa();
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    public void Store(UserId userId, SessionTokens tokens)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(tokens, SessionJsonContext.Default.SessionTokens);
        var sessionKey = RandomNumberGenerator.GetBytes(SessionKeySize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[json.Length];
        var tag = new byte[TagSize];

        try
        {
            using var aes = new AesGcm(sessionKey, TagSize);
            aes.Encrypt(nonce, json, ciphertext, tag);

            var blob = new TpmProtectedSessionBlob(
                1,
                WrapSessionKey(sessionKey),
                nonce,
                tag,
                ciphertext);

            var payload = JsonSerializer.SerializeToUtf8Bytes(blob, SessionJsonContext.Default.TpmProtectedSessionBlob);
            var path = SessionPath(userId);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, payload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
            CryptographicOperations.ZeroMemory(sessionKey);
        }
    }

    public SessionTokens? TryGet(UserId userId)
    {
        var path = SessionPath(userId);
        if (!File.Exists(path))
        {
            return null;
        }

        using var rsa = OpenRsa();

        try
        {
            var payload = File.ReadAllBytes(path);
            var blob = JsonSerializer.Deserialize(payload, SessionJsonContext.Default.TpmProtectedSessionBlob);
            if (blob is null || blob.Version != 1 || !IsValid(blob))
            {
                return null;
            }

            var sessionKey = rsa.Decrypt(blob.WrappedKey, RSAEncryptionPadding.OaepSHA256);
            if (sessionKey.Length != SessionKeySize)
            {
                CryptographicOperations.ZeroMemory(sessionKey);
                return null;
            }

            var json = new byte[blob.Ciphertext.Length];

            try
            {
                using var aes = new AesGcm(sessionKey, TagSize);
                aes.Decrypt(blob.Nonce, blob.Ciphertext, blob.Tag, json);

                return JsonSerializer.Deserialize(json, SessionJsonContext.Default.SessionTokens);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(json);
                CryptographicOperations.ZeroMemory(sessionKey);
            }
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Remove(UserId userId)
    {
        var path = SessionPath(userId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static byte[] WrapSessionKey(byte[] sessionKey)
    {
        using var rsa = OpenRsa();
        return rsa.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);
    }

    private static RSA OpenRsa()
    {
        return new RSACng(OpenOrCreateKey());
    }

    private static CngKey OpenOrCreateKey()
    {
        var provider = new CngProvider(TpmProviderName);
        if (CngKey.Exists(TpmKeyName, provider))
        {
            return CngKey.Open(TpmKeyName, provider);
        }

        var creationParameters = new CngKeyCreationParameters
        {
            Provider = provider,
            KeyUsage = CngKeyUsages.AllUsages,
            ExportPolicy = CngExportPolicies.None
        };

        creationParameters.Parameters.Add(
            new CngProperty("Length", BitConverter.GetBytes(RsaKeySizeBits), CngPropertyOptions.None));

        return CngKey.Create(CngAlgorithm.Rsa, TpmKeyName, creationParameters);
    }

    private static string SessionPath(UserId userId) =>
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "Sessions", $"{userId}.tpm");

    private static bool IsValid(TpmProtectedSessionBlob blob)
    {
        return blob.WrappedKey.Length > 0
            && blob.Nonce.Length == NonceSize
            && blob.Tag.Length == TagSize
            && blob.Ciphertext.Length > 0;
    }
}
