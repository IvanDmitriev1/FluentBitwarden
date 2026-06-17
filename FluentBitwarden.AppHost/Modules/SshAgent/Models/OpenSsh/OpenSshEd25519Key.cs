using BitwardenApi.Infrastructure.Encoding;
using FluentBitwarden.AppHost.Modules.SshAgent.Internal;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Models.OpenSsh;

internal readonly ref struct OpenSshEd25519Key
{
    public const string AlgorithmName = "ssh-ed25519";

    private const string BeginMarker = "-----BEGIN OPENSSH PRIVATE KEY-----";
    private const string EndMarker = "-----END OPENSSH PRIVATE KEY-----";

    public OpenSshEd25519Key(
        ReadOnlySpan<byte> seed,
        ReadOnlySpan<byte> publicKey,
        ReadOnlySpan<byte> publicKeyBlob)
    {
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seed must be 32 bytes.", nameof(seed));

        if (publicKey.Length != 32)
            throw new ArgumentException("Ed25519 public key must be 32 bytes.", nameof(publicKey));

        Seed = seed;
        PublicKey = publicKey;
        PublicKeyBlob = publicKeyBlob;
    }

    public ReadOnlySpan<byte> Seed { get; }
    public ReadOnlySpan<byte> PublicKey { get; }
    public ReadOnlySpan<byte> PublicKeyBlob { get; }

    public byte[] Sign(ReadOnlyMemory<byte> data)
    {
        var privateKey = new Ed25519PrivateKeyParameters(Seed);

        var signer = new Ed25519Signer();
        signer.Init(forSigning: true, privateKey);
        signer.BlockUpdate(data.Span);

        return signer.GenerateSignature();
    }

    public static OpenSshEd25519Key Parse(ReadOnlyMemory<char> privateKeyPem)
    {
        ReadOnlySpan<char> pem = privateKeyPem.Span;

        if (!TryGetPemBody(pem, out var base64Body) ||
            !Base64Extensions.TryConvertFromBase64Chars(base64Body, out var decodedBase64))
        {
            throw new ArgumentException(
                "Failed to parse OpenSshEd25519Key from string.",
                nameof(privateKeyPem));
        }

        var reader = new SshBinaryReader(decodedBase64);

        reader.ReadRaw("openssh-key-v1\0"u8);

        reader.ReadString("none"u8); // cipherName
        reader.ReadString("none"u8); // kdfName

        ReadOnlySpan<byte> kdfOptions = reader.ReadString();
        if (kdfOptions.Length != 0)
            throw new InvalidDataException("Unexpected KDF options.");

        uint numberOfKeys = reader.ReadUInt32();
        if (numberOfKeys != 1)
            throw new InvalidDataException("Expected exactly one key.");

        ReadOnlySpan<byte> publicKeyBlob = reader.ReadString();
        ReadOnlySpan<byte> privateBlock = reader.ReadString();

        if (!reader.End)
            throw new InvalidDataException("Unexpected trailing data.");

        return ParsePrivateBlock(publicKeyBlob, privateBlock);
    }

    private static bool TryGetPemBody(
        ReadOnlySpan<char> pem,
        out ReadOnlySpan<char> body)
    {
        body = default;

        int begin = pem.IndexOf(BeginMarker);
        if (begin < 0)
            return false;

        begin += BeginMarker.Length;

        int end = pem.IndexOf(EndMarker);
        if (end < 0 || end <= begin)
            return false;

        body = pem.Slice(begin, end - begin);
        return true;
    }

    private static OpenSshEd25519Key ParsePrivateBlock(
        ReadOnlySpan<byte> publicKeyBlob,
        ReadOnlySpan<byte> privateBlock)
    {
        var reader = new SshBinaryReader(privateBlock);

        uint check1 = reader.ReadUInt32();
        uint check2 = reader.ReadUInt32();

        if (check1 != check2)
            throw new InvalidDataException("OpenSSH private key check values do not match.");

        reader.ReadString("ssh-ed25519"u8);

        ReadOnlySpan<byte> publicKey = reader.ReadString();
        if (publicKey.Length != 32)
            throw new InvalidDataException("Invalid Ed25519 public key length.");

        ReadOnlySpan<byte> privateKey = reader.ReadString();
        if (privateKey.Length != 64)
            throw new InvalidDataException("Invalid Ed25519 private key length.");

        ReadOnlySpan<byte> seed = privateKey[..32];
        ReadOnlySpan<byte> embeddedPublicKey = privateKey.Slice(32, 32);

        if (!embeddedPublicKey.SequenceEqual(publicKey))
            throw new InvalidDataException("Embedded Ed25519 public key does not match public key.");

        ValidatePublicKeyBlob(publicKeyBlob, publicKey);

        _ = reader.ReadString(); // comment

        if (!ValidatePadding(reader.Remaining))
            throw new InvalidDataException("Invalid OpenSSH private key padding.");

        return new OpenSshEd25519Key(
            seed,
            publicKey,
            publicKeyBlob);
    }

    private static void ValidatePublicKeyBlob(
        ReadOnlySpan<byte> publicKeyBlob,
        ReadOnlySpan<byte> expectedPublicKey)
    {
        var reader = new SshBinaryReader(publicKeyBlob);

        reader.ReadString("ssh-ed25519"u8);

        ReadOnlySpan<byte> publicKey = reader.ReadString();
        if (!publicKey.SequenceEqual(expectedPublicKey))
            throw new InvalidDataException("Public key blob does not match expected public key.");

        if (!reader.End)
            throw new InvalidDataException("Unexpected trailing data in public key blob.");
    }

    private static bool ValidatePadding(ReadOnlySpan<byte> padding)
    {
        for (int i = 0; i < padding.Length; i++)
        {
            if (padding[i] != (byte)(i + 1))
                return false;
        }

        return true;
    }
}