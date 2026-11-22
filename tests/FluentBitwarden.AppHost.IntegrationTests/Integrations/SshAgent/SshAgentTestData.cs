
using System.Buffers.Binary;
using System.Text;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

internal static class SshAgentTestData
{
    public static byte[] EncodeUInt32(uint value)
    {
        byte[] result = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(result, value);
        return result;
    }

    public static byte[] EncodeString(ReadOnlySpan<byte> value) =>
        Concat(EncodeUInt32((uint)value.Length), value.ToArray());

    public static byte[] EncodeString(string value) => EncodeString(Encoding.UTF8.GetBytes(value));

    public static byte[] EncodeSignRequest(
        ReadOnlySpan<byte> publicKeyBlob,
        ReadOnlySpan<byte> data,
        uint flags) =>
        Concat(EncodeString(publicKeyBlob), EncodeString(data), EncodeUInt32(flags));

    public static byte[] EncodeExtensionRequest(string extensionName, ReadOnlySpan<byte> data) =>
        Concat(EncodeString(extensionName), data.ToArray());

    public static SshAgentMessage CreateForwardingNotification() =>
        new(SshAgentMessageType.Extension,
            EncodeExtensionRequest("session-bind@openssh.com",
                Concat(EncodeString([0x01]), EncodeString([0x02]), EncodeString([0x03]), [0x01])));

    public static byte[] EncodeUserAuthRequest(
        ReadOnlySpan<byte> sessionId,
        ReadOnlySpan<byte> usernameBytes,
        string method,
        ReadOnlySpan<byte> requestedClientKeyBlob,
        byte[]? hostKeyBlob = null) =>
        Concat(
            EncodeString(sessionId),
            [50],
            EncodeString(usernameBytes),
            EncodeString("ssh-connection"),
            EncodeString(method),
            [1],
            EncodeString("ssh-ed25519"),
            EncodeString(requestedClientKeyBlob),
            hostKeyBlob is null ? Array.Empty<byte>() : EncodeString(hostKeyBlob));

    public static (byte[] Payload, byte[] HostKeyBlob) EncodeSessionBinding(
        ReadOnlySpan<byte> sessionId,
        string hostKeyAlgorithm = "ssh-ed25519",
        byte signingSeedByte = 0xA7)
    {
        byte[] signingSeed = Enumerable.Range(0, Ed25519.SecretKeySize)
            .Select(index => (byte)(signingSeedByte + index))
            .ToArray();
        byte[] publicKey = new byte[Ed25519.PublicKeySize];
        Ed25519.GeneratePublicKey(signingSeed, 0, publicKey, 0);
        byte[] hostKeyBlob = Concat(EncodeString(hostKeyAlgorithm), EncodeString(publicKey));
        byte[] signature = new byte[Ed25519.SignatureSize];
        byte[] session = sessionId.ToArray();
        Ed25519.Sign(signingSeed, 0, session, 0, session.Length, signature, 0);
        byte[] signatureBlob = Concat(EncodeString("ssh-ed25519"), EncodeString(signature));

        return (
            Concat(EncodeString(hostKeyBlob), EncodeString(session), EncodeString(signatureBlob), [0]),
            hostKeyBlob);
    }

    public static byte[] CreateHostKeyBlob(byte seedByte = 0x61)
    {
        byte[] seed = Enumerable.Range(0, Ed25519.SecretKeySize)
            .Select(index => (byte)(seedByte + index))
            .ToArray();
        byte[] publicKey = new byte[Ed25519.PublicKeySize];
        Ed25519.GeneratePublicKey(seed, 0, publicKey, 0);
        return Concat(EncodeString("ssh-ed25519"), EncodeString(publicKey));
    }

    public static byte[] Concat(params byte[][] parts)
    {
        byte[] result = new byte[parts.Sum(static part => part.Length)];
        int offset = 0;

        foreach (byte[] part in parts)
        {
            part.CopyTo(result, offset);
            offset += part.Length;
        }

        return result;
    }

    public static SyntheticOpenSshKey CreateKey(byte seedByte = 0x31)
    {
        byte[] seed = Enumerable.Range(0, Ed25519PrivateKeyParameters.KeySize)
            .Select(index => (byte)(seedByte + index))
            .ToArray();
        var privateKey = new Ed25519PrivateKeyParameters(seed);
        Ed25519PublicKeyParameters publicKey = privateKey.GeneratePublicKey();
        byte[] publicKeyBytes = publicKey.GetEncoded();
        byte[] publicKeyBlob = Concat(EncodeString("ssh-ed25519"), EncodeString(publicKeyBytes));
        byte[] privateKeyBytes = Concat(seed, publicKeyBytes);

        using var privateSection = new MemoryStream();
        WriteUInt32(privateSection, 0xA1B2C3D4);
        WriteUInt32(privateSection, 0xA1B2C3D4);
        WriteString(privateSection, "ssh-ed25519"u8);
        WriteString(privateSection, publicKeyBytes);
        WriteString(privateSection, privateKeyBytes);
        WriteString(privateSection, "synthetic key"u8);

        for (byte padding = 1; privateSection.Length % 8 != 0; padding++)
            privateSection.WriteByte(padding);

        using var keyBlob = new MemoryStream();
        keyBlob.Write("openssh-key-v1\0"u8);
        WriteString(keyBlob, "none"u8);
        WriteString(keyBlob, "none"u8);
        WriteString(keyBlob, ReadOnlySpan<byte>.Empty);
        WriteUInt32(keyBlob, 1);
        WriteString(keyBlob, publicKeyBlob);
        WriteString(keyBlob, privateSection.ToArray());

        string pem = "-----BEGIN OPENSSH PRIVATE KEY-----\n" +
            Convert.ToBase64String(keyBlob.ToArray(), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END OPENSSH PRIVATE KEY-----\n";

        return new SyntheticOpenSshKey(pem, publicKeyBlob, publicKey);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteString(Stream stream, ReadOnlySpan<byte> value)
    {
        WriteUInt32(stream, (uint)value.Length);
        stream.Write(value);
    }
}

internal sealed record SyntheticOpenSshKey(
    string PrivateKeyPem,
    byte[] PublicKeyBlob,
    Ed25519PublicKeyParameters PublicKey)
{
    public SshKeyCipher CreateCipher(string name = "synthetic key") => new()
    {
        Id = CipherId.Empty,
        Name = name,
        Favorite = false,
        Reprompt = false,
        RevisionDate = DateTimeOffset.UnixEpoch,
        CreationDate = DateTimeOffset.UnixEpoch,
        DeletedDate = null,
        PublicKey = new OpenSshPublicKey(
            $"ssh-ed25519 {Convert.ToBase64String(PublicKeyBlob)}",
            PublicKeyBlob),
        PrivateKey = PrivateKeyPem,
        KeyFingerprint = "SHA256:synthetic"
    };
}
