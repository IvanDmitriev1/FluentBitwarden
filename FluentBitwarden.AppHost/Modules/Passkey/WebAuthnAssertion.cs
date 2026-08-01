using System.Buffers.Binary;
using System.Security.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Passkey;

internal static class WebAuthnAssertion
{
    [Flags]
    enum WebAuthnAssertionFlags : byte
    {
        UserPresent = 0x01,
        UserVerified = 0x04,
        BackupEligible = 0x08,
        BackupState = 0x10
    }

    public static byte[] BuildAuthenticatorData(
        byte[] rpIdHash,
        int counter,
        bool userVerified,
        bool backedUpPasskey)
    {
        // 32-byte rpIdHash + 1-byte flags + 4-byte signCount.
        var authenticatorData = new byte[37];
        Buffer.BlockCopy(rpIdHash, 0, authenticatorData, 0, 32);

        WebAuthnAssertionFlags flags = WebAuthnAssertionFlags.UserPresent;

        if (userVerified)
        {
            flags |= WebAuthnAssertionFlags.UserVerified;
        }

        if (backedUpPasskey)
        {
            flags |= WebAuthnAssertionFlags.BackupEligible;
            flags |= WebAuthnAssertionFlags.BackupState;
        }

        authenticatorData[32] = (byte)flags;

        // WebAuthn signCount is big-endian.
        BinaryPrimitives.WriteUInt32BigEndian(
            authenticatorData.AsSpan(33, 4),
            checked((uint)Math.Max(counter, 0)));

        return authenticatorData;
    }

    public static byte[] BuildSignedPayload(byte[] authenticatorData, byte[] clientDataHash)
    {
        if (authenticatorData.Length < 37)
        {
            throw new ArgumentException("Authenticator data is too short.", nameof(authenticatorData));
        }

        if (clientDataHash.Length != 32)
        {
            throw new ArgumentException("Client data hash must be exactly 32 bytes.", nameof(clientDataHash));
        }

        var payload = new byte[authenticatorData.Length + clientDataHash.Length];

        Buffer.BlockCopy(authenticatorData, 0, payload, 0, authenticatorData.Length);
        Buffer.BlockCopy(clientDataHash, 0, payload, authenticatorData.Length, clientDataHash.Length);

        return payload;
    }

    public static byte[] SignEs256(byte[] privateKey, byte[] payload)
    {
        using var ecdsa = ECDsa.Create();

        if (!TryImportEcPrivateKey(ecdsa, privateKey))
        {
            throw new CryptographicException(
                "Unsupported passkey private key format. Expected PKCS#8 or SEC1 EC private key.");
        }

        return ecdsa.SignData(
            payload,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
    }

    private static bool TryImportEcPrivateKey(ECDsa ecdsa, byte[] privateKey)
    {
        try
        {
            ecdsa.ImportPkcs8PrivateKey(privateKey, out var bytesRead);
            return bytesRead == privateKey.Length;
        }
        catch (CryptographicException)
        {
            // Try SEC1 below.
        }

        try
        {
            ecdsa.ImportECPrivateKey(privateKey, out var bytesRead);
            return bytesRead == privateKey.Length;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
