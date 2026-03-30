using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Internal;
using FluentBitwarden.Shared.Extensions;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Services;

internal sealed class TpmSecretProtector : ISecretProtector
{
    private const int SessionKeySize = 32;

    public static bool IsAvailable => CheckIsAvailable();

    public void Protect(string filePath, ReadOnlySpan<byte> plaintext)
    {
        FilePathHelpers.EnsureParentDirectoryExists(filePath);
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 128);
        ProtectPayload(stream, plaintext);
    }

    public byte[]? TryUnprotect(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var protectedPayloadOwner = FilePathHelpers.ReadAllBytesOwner(filePath);

        try
        {
            var protectedPayload = protectedPayloadOwner.Span;
            return TryUnprotectPayload(protectedPayload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedPayloadOwner.Span);
        }
    }

    private static void ProtectPayload(Stream destination, ReadOnlySpan<byte> plaintext)
    {
        Span<byte> sessionKey = stackalloc byte[SessionKeySize];
        RandomNumberGenerator.Fill(sessionKey);

        using var protectedPayloadOwner = TpmProtectedPayloadOwner.Create(plaintext.Length);

        using var aes = new AesGcm(sessionKey, TpmProtectedPayloadCodec.TagSize);
        aes.Encrypt(protectedPayloadOwner.Nonce, plaintext, protectedPayloadOwner.Ciphertext, protectedPayloadOwner.Tag);

        using var rsa = TpmRsaFactory.OpenRsa();
        using var wrappedKeyOwner = MemoryOwner<byte>.Allocate(rsa.KeySize / 8);
        var wrappedKey = wrappedKeyOwner.Span[..(rsa.KeySize / 8)];

        try
        {
            if (!rsa.TryEncrypt(sessionKey, wrappedKey, RSAEncryptionPadding.OaepSHA256, out int wrappedKeyLength))
            {
                throw new CryptographicException("TPM RSA encryption failed.");
            }

            var payload = protectedPayloadOwner.CreatePayload(wrappedKey[..wrappedKeyLength]);
            TpmProtectedPayloadCodec.Write(destination, payload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(wrappedKey);
        }
    }

    private static byte[]? TryUnprotectPayload(ReadOnlySpan<byte> protectedPayload)
    {
        if (!TpmProtectedPayloadCodec.TryDecode(protectedPayload, out var payload))
        {
            return null;
        }

        try
        {
            using var rsa = TpmRsaFactory.OpenRsa();
            Span<byte> sessionKey = stackalloc byte[SessionKeySize];

            if (!rsa.TryDecrypt(payload.WrappedKey, sessionKey, RSAEncryptionPadding.OaepSHA256, out int bytesWritten)
                || bytesWritten != SessionKeySize)
            {
                return null;
            }

            var plaintext = new byte[payload.Ciphertext.Length];
            using var aes = new AesGcm(sessionKey, TpmProtectedPayloadCodec.TagSize);
            aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext);
            return plaintext;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static bool CheckIsAvailable()
    {
        try
        {
            using var rsa = TpmRsaFactory.OpenRsa();
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
}
