using System.Buffers.Binary;
using System.Security.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Passkey.Internal;

internal static class WebAuthnAssertion
{
    [Flags]
    private enum AuthenticatorFlags : byte
    {
        UserPresent = 0x01,
        UserVerified = 0x04,
        BackupEligible = 0x08,
        BackupState = 0x10
    }

    private const int HashLength = 32;
    private const int AuthenticatorDataLength = 37;

    public static (byte[] AuthenticatorData, byte[] Signature) Create(
        ReadOnlySpan<byte> rpIdHash,
        ReadOnlySpan<byte> clientDataHash,
        ReadOnlySpan<byte> privateKey,
        uint signCount)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(rpIdHash.Length, HashLength);
        ArgumentOutOfRangeException.ThrowIfNotEqual(clientDataHash.Length, HashLength);

        var authenticatorData = new byte[AuthenticatorDataLength];

        rpIdHash.CopyTo(authenticatorData);

        AuthenticatorFlags flags = AuthenticatorFlags.UserPresent;
        flags |= AuthenticatorFlags.UserVerified;
        flags |= AuthenticatorFlags.BackupEligible;
        flags |= AuthenticatorFlags.BackupState;

        authenticatorData[32] = (byte)flags;
        BinaryPrimitives.WriteUInt32BigEndian(authenticatorData.AsSpan(33), signCount);

        Span<byte> signedData = stackalloc byte[AuthenticatorDataLength + HashLength];
        authenticatorData.CopyTo(signedData);
        clientDataHash.CopyTo(signedData[AuthenticatorDataLength..]);

        Span<byte> hash = stackalloc byte[HashLength];
        SHA256.HashData(signedData, hash);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKey, out var bytesRead);

        if (bytesRead != privateKey.Length)
            throw new CryptographicException("Invalid passkey private key.");

        var signature = ecdsa.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);
        return (authenticatorData, signature);
    }
}
