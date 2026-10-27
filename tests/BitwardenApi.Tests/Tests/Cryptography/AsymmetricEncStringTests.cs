using System.Security.Cryptography;
using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests.Cryptography;

public sealed class AsymmetricEncStringTests
{
    [Theory]
    [InlineData(EncryptionType.Rsa2048_OaepSha256_B64)]
    [InlineData(EncryptionType.Rsa2048_OaepSha1_B64)]
    public void Decode_supports_both_unsigned_rsa_oaep_types(EncryptionType type)
    {
        using RSA rsa = CreateRsa();
        byte[] plaintext = [0, 1, 2, 3, 0xfe, 0xff];
        byte[] ciphertext = rsa.Encrypt(plaintext, PaddingFor(type));
        AsymmetricEncString value = ReadAsymmetricString($"{(byte)type}.{Convert.ToBase64String(ciphertext)}");
        byte[] destination = Enumerable.Repeat((byte)0xff, 512).ToArray();

        Assert.Equal(ciphertext.Length, value.MaxPlaintextByteCount);
        Assert.False(value.IsEmpty);
        Assert.Equal(plaintext.Length, value.DecodeRsaTo(rsa, destination));
        Assert.Equal(plaintext, destination[..plaintext.Length]);
        Assert.All(destination[plaintext.Length..], static item => Assert.Equal((byte)0, item));

        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        try
        {
            using RsaPrivateKey imported = RsaPrivateKey.Import(privateKeyBytes);
            Assert.Equal(plaintext, imported.Decrypt(value));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBytes);
        }
    }

    [Fact]
    public void Asymmetric_enc_string_reads_null_but_writer_rejects_empty_and_nonempty_values()
    {
        AsymmetricEncString empty = ReadAsymmetricJson("null");

        Assert.True(empty.IsEmpty);
        Assert.Equal(AsymmetricEncString.Empty, empty);
        Assert.Throws<NotSupportedException>(() => WriteAsymmetric(empty));
        Assert.Throws<NotSupportedException>(() => WriteAsymmetric(AsymmetricEncString.FromBytes(
            CryptoTestFixtures.Pack(
                EncryptionType.Rsa2048_OaepSha256_B64,
                ReadOnlySpan<byte>.Empty,
                [1],
                ReadOnlySpan<byte>.Empty))));
    }

    [Fact]
    public void Asymmetric_enc_string_equality_matches_its_packed_value()
    {
        byte[] packed = CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            ReadOnlySpan<byte>.Empty,
            [1, 2, 3],
            ReadOnlySpan<byte>.Empty);
        AsymmetricEncString first = AsymmetricEncString.FromBytes(packed);
        AsymmetricEncString second = AsymmetricEncString.FromBytes(packed.ToArray());

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.True(first.Equals((object)second));
        Assert.False(first.Equals((object?)null));
        Assert.False(first.Equals((object?)"not an AsymmetricEncString"));
    }

    [Fact]
    public void Rsa_private_key_import_supports_pkcs1_fallback_and_rejects_invalid_data()
    {
        using RSA rsa = CreateRsa();
        byte[] privateKeyBytes = rsa.ExportRSAPrivateKey();
        byte[] plaintext = [7, 8, 9];
        byte[] ciphertext = rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256);
        AsymmetricEncString wrapped = AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            ReadOnlySpan<byte>.Empty,
            ciphertext,
            ReadOnlySpan<byte>.Empty));

        try
        {
            using RsaPrivateKey imported = RsaPrivateKey.Import(privateKeyBytes);
            Assert.Equal(plaintext, imported.Decrypt(wrapped));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBytes);
        }

        Assert.Throws<CryptographicException>(() => RsaPrivateKey.Import(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void Rsa_private_key_decrypt_checks_disposal_and_destination_capacity()
    {
        using RSA rsa = CreateRsa();
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        byte[] ciphertext = rsa.Encrypt([1, 2, 3, 4, 5, 6], RSAEncryptionPadding.OaepSHA256);
        AsymmetricEncString wrapped = AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            ReadOnlySpan<byte>.Empty,
            ciphertext,
            ReadOnlySpan<byte>.Empty));
        RsaPrivateKey privateKey;

        try
        {
            privateKey = RsaPrivateKey.Import(privateKeyBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKeyBytes);
        }

        Assert.Throws<ArgumentException>(() => privateKey.Decrypt(wrapped, new byte[1]));
        Assert.Throws<ArgumentException>(() => wrapped.DecodeRsaTo(rsa, new byte[1]));
        privateKey.Dispose();
        privateKey.Dispose();
        Assert.Throws<ObjectDisposedException>(() => privateKey.Decrypt(wrapped));
        Assert.Throws<ObjectDisposedException>(() => privateKey.Decrypt(wrapped, new byte[256]));
    }

    [Fact]
    public void Rsa_decryption_rejects_wrong_keys_signed_types_and_unexpected_segments()
    {
        using RSA sender = CreateRsa();
        using RSA other = CreateRsa();
        byte[] ciphertext = sender.Encrypt([1, 2, 3], RSAEncryptionPadding.OaepSHA256);
        byte[] sha1Ciphertext = sender.Encrypt([1, 2, 3], RSAEncryptionPadding.OaepSHA1);
        AsymmetricEncString wrapped = AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            ReadOnlySpan<byte>.Empty,
            ciphertext,
            ReadOnlySpan<byte>.Empty));

        Assert.Throws<CryptographicException>(() => wrapped.DecodeRsaTo(other, new byte[256]));
        Assert.Throws<CryptographicException>(() => Decode(sender, EncryptionType.Rsa2048_OaepSha256_HmacSha256_B64, ciphertext));
        Assert.Throws<CryptographicException>(() => Decode(sender, EncryptionType.Rsa2048_OaepSha1_HmacSha256_B64, sha1Ciphertext));
        Assert.Throws<CryptographicException>(() => Decode(sender, EncryptionType.AesCbc256_B64, ciphertext));
        Assert.Throws<CryptographicException>(() => Decode(sender, EncryptionType.AesCbc256_HmacSha256_B64, ciphertext));
        Assert.Throws<CryptographicException>(() => DecodeWithUnexpectedIv(sender, ciphertext));
        Assert.Throws<CryptographicException>(() => DecodeWithUnexpectedMac(sender, ciphertext));
    }

    [Fact]
    public void Asymmetric_enc_string_converter_rejects_non_string_tokens()
    {
        Assert.Throws<JsonException>(() => ReadAsymmetricJson("true"));
    }

    [Theory]
    [InlineData(EncryptionType.Rsa2048_OaepSha256_HmacSha256_B64)]
    [InlineData(EncryptionType.Rsa2048_OaepSha1_HmacSha256_B64)]
    public void Parsing_accepts_signed_rsa_wire_types_for_later_rejection(EncryptionType type)
    {
        string encoded = $"{(byte)type}.AQID|BAUG";

        AsymmetricEncString value = ReadAsymmetricString(encoded);

        Assert.Equal(3, value.MaxPlaintextByteCount);
        Assert.Equal(
            CryptoTestFixtures.Pack(type, ReadOnlySpan<byte>.Empty, [1, 2, 3], [4, 5, 6]),
            value.ToByteArray());
    }

    private static void Decode(RSA rsa, EncryptionType type, byte[] ciphertext)
    {
        AsymmetricEncString value = AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            type,
            ReadOnlySpan<byte>.Empty,
            ciphertext,
            type is EncryptionType.Rsa2048_OaepSha256_HmacSha256_B64 or EncryptionType.Rsa2048_OaepSha1_HmacSha256_B64
                ? new byte[32]
                : ReadOnlySpan<byte>.Empty));
        _ = value.DecodeRsaTo(rsa, new byte[256]);
    }

    private static void DecodeWithUnexpectedIv(RSA rsa, byte[] ciphertext)
    {
        AsymmetricEncString value = AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            new byte[16],
            ciphertext,
            ReadOnlySpan<byte>.Empty));
        _ = value.DecodeRsaTo(rsa, new byte[256]);
    }

    private static void DecodeWithUnexpectedMac(RSA rsa, byte[] ciphertext)
    {
        AsymmetricEncString value = AsymmetricEncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            ReadOnlySpan<byte>.Empty,
            ciphertext,
            new byte[32]));
        _ = value.DecodeRsaTo(rsa, new byte[256]);
    }

    private static AsymmetricEncString ReadAsymmetricString(string encoded) => ReadAsymmetricJson($"\"{encoded}\"");

    private static AsymmetricEncString ReadAsymmetricJson(string json)
    {
        Utf8JsonReader reader = new(System.Text.Encoding.UTF8.GetBytes(json));
        Assert.True(reader.Read());
        return new AsymmetricEncStringJsonConverter().Read(
            ref reader,
            typeof(AsymmetricEncString),
            new JsonSerializerOptions());
    }

    private static string WriteAsymmetric(AsymmetricEncString value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            new AsymmetricEncStringJsonConverter().Write(writer, value, new JsonSerializerOptions());
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static RSA CreateRsa()
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = 2048;
        return rsa;
    }

    private static RSAEncryptionPadding PaddingFor(EncryptionType type) => type switch
    {
        EncryptionType.Rsa2048_OaepSha256_B64 => RSAEncryptionPadding.OaepSHA256,
        EncryptionType.Rsa2048_OaepSha1_B64 => RSAEncryptionPadding.OaepSHA1,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
