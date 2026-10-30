using System.Buffers.Binary;
using System.Security.Cryptography;
using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests.Cryptography;

public sealed class EncStringTests
{
    [Fact]
    public void Decode_supports_legacy_aes_cbc_without_hmac_against_a_fixed_vector()
    {
        string encoded = $"{CryptoTestFixtures.LegacyAesIvBase64}|{CryptoTestFixtures.LegacyAesCiphertextBase64}";
        EncString value = TestApiSupport.ParseEncString(encoded);
        byte[] destination = new byte[32];
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("legacy AES-CBC fixture");

        Assert.Equal("legacy AES-CBC fixture", value.Decode(CryptoTestFixtures.AesKey()));
        Assert.Equal(expected, value.DecodeToArray(CryptoTestFixtures.AesKey()));
        Assert.Equal(expected.Length, value.DecodeTo(CryptoTestFixtures.AesKey(), destination));
        Assert.Equal(expected, destination[..expected.Length]);
    }

    [Fact]
    public void Authenticated_fixed_vector_decodes_through_string_array_and_span_apis()
    {
        EncString value = TestApiSupport.ParseEncString(CryptoTestFixtures.AuthenticatedEncString);
        byte[] key = CryptoTestFixtures.CombinedKey();
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("fixture plaintext");
        byte[] destination = Enumerable.Repeat((byte)0xff, 48).ToArray();

        Assert.Equal("fixture plaintext", value.Decode(key));
        Assert.Equal(expected, value.DecodeToArray(key));
        Assert.Equal(expected.Length, value.DecodeTo(key, destination));
        Assert.Equal(expected, destination[..expected.Length]);
        Assert.All(destination[expected.Length..], static value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public void Encrypt_to_matches_the_fixed_authenticated_reference_vector()
    {
        byte[] key = CryptoTestFixtures.CombinedKey();
        byte[] iv = Convert.FromBase64String(CryptoTestFixtures.AuthenticatedIvBase64);
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("fixture plaintext");
        byte[] ciphertext = new byte[32];
        byte[] mac = new byte[32];

        int written = AesCbcHmac.EncryptTo(plaintext, key, iv, ciphertext, mac);

        Assert.Equal(
            CryptoTestFixtures.AuthenticatedCiphertextBase64,
            Convert.ToBase64String(ciphertext.AsSpan(0, written)));
        Assert.Equal(CryptoTestFixtures.AuthenticatedMacBase64, Convert.ToBase64String(mac));
    }

    [Fact]
    public void EncString_encrypts_utf8_text_and_handles_stack_and_heap_buffer_boundaries()
    {
        byte[] key = CryptoTestFixtures.CombinedKey();

        foreach (int plaintextByteCount in new[] { 496, 512 })
        {
            string plaintext = new('x', plaintextByteCount);
            EncString value = EncString.Encrypt(plaintext, key);

            Assert.Equal(plaintextByteCount, value.Decode(key).Length);
            Assert.Equal(plaintextByteCount <= 496 ? 512 : 528, value.MaxPlaintextByteCount);

            string json = WriteEncString(value);
            Assert.Equal(plaintext, ReadEncStringJson(json).Decode(key));
        }

        Assert.Equal("secret \u2713", EncString.Encrypt("secret \u2713", key).Decode(key));
    }

    [Fact]
    public void EncString_json_converter_reads_null_and_writes_null_for_empty_values()
    {
        EncString empty = ReadEncStringJson("null");

        Assert.True(empty.IsEmpty);
        Assert.Equal(EncString.Empty, empty);
        Assert.Equal("null", WriteEncString(empty));
        Assert.Equal(string.Empty, empty.Decode([]));
    }

    [Fact]
    public void EncString_json_converter_round_trips_canonical_wire_format()
    {
        EncString value = TestApiSupport.ParseEncString(CryptoTestFixtures.AuthenticatedEncString);

        string json = WriteEncString(value);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(CryptoTestFixtures.AuthenticatedEncString, document.RootElement.GetString());
        Assert.Equal(value, ReadEncStringJson(json));
    }

    [Fact]
    public void EncString_json_converter_writes_legacy_values_without_a_mac_segment()
    {
        string encoded = $"0.{CryptoTestFixtures.LegacyAesIvBase64}|{CryptoTestFixtures.LegacyAesCiphertextBase64}";
        EncString value = TestApiSupport.ParseEncString(encoded);

        string json = WriteEncString(value);

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(encoded, document.RootElement.GetString());
        Assert.Equal(value, ReadEncStringJson(json));
    }

    [Fact]
    public void EncString_json_converter_rejects_non_string_tokens()
    {
        Assert.Throws<JsonException>(() => ReadEncStringJson("42"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2.")]
    [InlineData("0.AQ==")]
    [InlineData("2.AQ==|AQ==")]
    [InlineData("9.AQ==")]
    [InlineData("x.AQ==|AQ==")]
    [InlineData("0a.AQ==|AQ==")]
    [InlineData("2.!|AQ==|AQ==")]
    [InlineData("2.AAECAwQFBgcICQoLDA0ODw==|!|AQ==")]
    [InlineData("2.AAECAwQFBgcICQoLDA0ODw==|AQ==|!")]
    [InlineData(".AQ==|AQ==")]
    public void Parsing_rejects_malformed_or_unsupported_encoded_segments(string encoded)
    {
        Assert.Throws<FormatException>(() => TestApiSupport.ParseEncString(encoded));
    }

    [Fact]
    public void Parsing_and_decoding_accept_base64url_segments_and_a_legacy_prefix_omission()
    {
        string iv = ToBase64Url(CryptoTestFixtures.LegacyAesIvBase64);
        string ciphertext = ToBase64Url(CryptoTestFixtures.LegacyAesCiphertextBase64);
        EncString prefixed = TestApiSupport.ParseEncString($"0.{iv}|{ciphertext}");
        EncString unprefixed = TestApiSupport.ParseEncString($"{iv}|{ciphertext}");

        Assert.Equal("legacy AES-CBC fixture", prefixed.Decode(CryptoTestFixtures.AesKey()));
        Assert.Equal(prefixed, unprefixed);
    }

    [Fact]
    public void Encoded_string_extension_decodes_without_mutating_input_or_preserves_only_the_plaintext_prefix()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes(CryptoTestFixtures.AuthenticatedEncString);
        byte[] inPlace = original.ToArray();
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("fixture plaintext");
        byte[] key = CryptoTestFixtures.CombinedKey();

        Assert.Equal("fixture plaintext", original.AsSpan().DecodeEncString(key));
        Assert.Equal(CryptoTestFixtures.AuthenticatedEncString, System.Text.Encoding.UTF8.GetString(original));

        int written = inPlace.AsSpan().DecodeEncStringInPlace(key);

        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, inPlace.AsSpan(0, written).ToArray());
    }

    [Fact]
    public void Encoded_string_extensions_decode_legacy_values_and_reject_unparseable_input()
    {
        byte[] encoded = System.Text.Encoding.UTF8.GetBytes(
            $"0.{CryptoTestFixtures.LegacyAesIvBase64}|{CryptoTestFixtures.LegacyAesCiphertextBase64}");
        byte[] inPlace = encoded.ToArray();
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("legacy AES-CBC fixture");
        byte[] key = CryptoTestFixtures.AesKey();
        int written = inPlace.AsSpan().DecodeEncStringInPlace(key);

        Assert.Equal("legacy AES-CBC fixture", encoded.AsSpan().DecodeEncString(key));
        Assert.Equal(expected, inPlace.AsSpan(0, written).ToArray());
        Assert.Throws<FormatException>(() => Array.Empty<byte>().AsSpan().DecodeEncString(key));
        Assert.Throws<FormatException>(() => Array.Empty<byte>().AsSpan().DecodeEncStringInPlace(key));
    }

    [Fact]
    public void Encoded_string_extensions_reach_the_missing_iv_path_and_reject_it_cryptographically()
    {
        string encoded = $"2.|AQ==|{Convert.ToBase64String(new byte[32])}";
        byte[] inPlace = System.Text.Encoding.UTF8.GetBytes(encoded);
        byte[] key = CryptoTestFixtures.CombinedKey();

        Assert.Throws<CryptographicException>(() => System.Text.Encoding.UTF8.GetBytes(encoded).AsSpan().DecodeEncString(key));
        Assert.Throws<CryptographicException>(() => inPlace.AsSpan().DecodeEncStringInPlace(key));
    }

    [Fact]
    public void Decoding_rejects_a_type_two_value_with_missing_iv_or_mac()
    {
        EncString missingIv = TestApiSupport.ParseEncString("2.|AQ==|AQ==");
        EncString missingMac = TestApiSupport.ParseEncString("2.AAECAwQFBgcICQoLDA0ODw==|AQ==|");
        byte[] key = CryptoTestFixtures.CombinedKey();

        Assert.Throws<CryptographicException>(() => missingIv.Decode(key));
        Assert.Throws<CryptographicException>(() => missingMac.Decode(key));
    }

    [Fact]
    public void Parsing_accepts_a_missing_type_two_iv_then_decode_rejects_it_cryptographically()
    {
        EncString value = EncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.AesCbc256_HmacSha256_B64,
            ReadOnlySpan<byte>.Empty,
            [1],
            new byte[32]));

        Assert.Throws<CryptographicException>(() => value.Decode(CryptoTestFixtures.CombinedKey()));
    }

    [Fact]
    public void Decoding_rejects_invalid_iv_mac_and_key_lengths()
    {
        byte[] data = [1];
        byte[] iv = new byte[16];
        byte[] mac = new byte[32];
        byte[] combinedKey = CryptoTestFixtures.CombinedKey();
        byte[] aesKey = CryptoTestFixtures.AesKey();

        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_B64, [], data, [] , aesKey));
        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_B64, new byte[15], data, [], aesKey));
        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_B64, iv, data, [], new byte[31]));
        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_HmacSha256_B64, iv, data, [], combinedKey));
        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_HmacSha256_B64, iv, data, new byte[1], combinedKey));
        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_HmacSha256_B64, iv, data, mac, new byte[63]));
        Assert.Throws<CryptographicException>(() => DecodePacked(
            EncryptionType.AesCbc256_HmacSha256_B64, iv, data, mac, new byte[64]));
    }

    [Fact]
    public void Decryption_rejects_aes_cbc_plaintext_with_invalid_pkcs7_padding()
    {
        byte[] key = CryptoTestFixtures.AesKey();
        byte[] iv = new byte[16];
        byte[] invalidPaddedPlaintext = new byte[16];
        byte[] ciphertext;
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
            ciphertext = encryptor.TransformFinalBlock(invalidPaddedPlaintext, 0, invalidPaddedPlaintext.Length);
        }

        EncString value = EncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.AesCbc256_B64,
            iv,
            ciphertext,
            ReadOnlySpan<byte>.Empty));

        Assert.Throws<CryptographicException>(() => value.Decode(key));
        Assert.Throws<CryptographicException>(() => value.DecodeToArray(key));
        Assert.Throws<CryptographicException>(() => value.DecodeTo(key, new byte[16]));
    }

    [Fact]
    public void Decoding_rejects_ciphertext_that_fails_aes_cbc_after_a_valid_mac()
    {
        byte[] key = CryptoTestFixtures.CombinedKey();
        byte[] iv = new byte[16];
        byte[] ciphertext = new byte[15];
        byte[] authenticatedBytes = [.. iv, .. ciphertext];
        byte[] mac = HMACSHA256.HashData(key.AsSpan(32, 32), authenticatedBytes);
        EncString value = EncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.AesCbc256_HmacSha256_B64,
            iv,
            ciphertext,
            mac));

        Assert.Throws<CryptographicException>(() => value.Decode(key));
    }

    [Fact]
    public void Encrypt_to_rejects_short_key_invalid_iv_short_mac_destination_and_short_ciphertext_destination()
    {
        byte[] key = CryptoTestFixtures.CombinedKey();
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("fixture plaintext");
        byte[] iv = new byte[16];

        Assert.Throws<CryptographicException>(() => AesCbcHmac.EncryptTo(plaintext, new byte[63], iv, new byte[32], new byte[32]));
        Assert.Throws<CryptographicException>(() => AesCbcHmac.EncryptTo(plaintext, key, new byte[15], new byte[32], new byte[32]));
        Assert.Throws<ArgumentException>(() => AesCbcHmac.EncryptTo(plaintext, key, iv, new byte[32], new byte[31]));
        Assert.Throws<ArgumentException>(() => AesCbcHmac.EncryptTo(plaintext, key, iv, new byte[16], new byte[32]));
    }

    [Fact]
    public void Decode_to_rejects_a_destination_that_cannot_hold_plaintext()
    {
        EncString value = TestApiSupport.ParseEncString(CryptoTestFixtures.AuthenticatedEncString);

        Assert.Throws<ArgumentException>(() => value.DecodeTo(CryptoTestFixtures.CombinedKey(), new byte[16]));
    }

    [Fact]
    public void Packed_enc_strings_validate_header_lengths_and_preserve_equality_semantics()
    {
        byte[] packed = CryptoTestFixtures.Pack(
            EncryptionType.AesCbc256_B64,
            Convert.FromBase64String(CryptoTestFixtures.LegacyAesIvBase64),
            Convert.FromBase64String(CryptoTestFixtures.LegacyAesCiphertextBase64),
            ReadOnlySpan<byte>.Empty);
        EncString value = EncString.FromBytes(packed);
        EncString same = EncString.FromBytes(packed.ToArray());
        byte[] negativeIvLength = packed.ToArray();
        byte[] negativeMacLength = packed.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(negativeIvLength.AsSpan(1, 4), -1);
        BinaryPrimitives.WriteInt32LittleEndian(negativeMacLength.AsSpan(9, 4), -1);

        Assert.Equal(packed, value.ToByteArray());
        Assert.Equal(32, value.MaxPlaintextByteCount);
        Assert.Equal(value, same);
        Assert.Equal(value.GetHashCode(), same.GetHashCode());
        Assert.True(value == same);
        Assert.False(value != same);
        Assert.True(value.Equals((object)same));
        Assert.False(value.Equals((object?)"not an EncString"));
        Assert.False(value.Equals(EncString.Empty));
        Assert.False(EncString.Empty.Equals(value));
        Assert.True(value != EncString.Empty);
        Assert.False(value == EncString.Empty);
        Assert.True(EncString.Empty.Equals(new EncString()));
        Assert.Equal(0, EncString.Empty.GetHashCode());
        Assert.Empty(EncString.Empty.ToByteArray());
        Assert.Empty(default(EncString).ToByteArray());
        Assert.Throws<FormatException>(() => EncString.FromBytes([]));
        Assert.Throws<FormatException>(() => EncString.FromBytes(new byte[12]));
        Assert.Throws<FormatException>(() => EncString.FromBytes(negativeIvLength));
        Assert.Throws<FormatException>(() => EncString.FromBytes(negativeMacLength));
        Assert.Throws<FormatException>(() => EncString.FromBytes(packed[..^1]));
        Assert.Throws<FormatException>(() => EncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.AesCbc256_B64,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty)));
    }

    [Fact]
    public void EncString_json_writing_rejects_rsa_encryption_types()
    {
        EncString rsaValue = EncString.FromBytes(CryptoTestFixtures.Pack(
            EncryptionType.Rsa2048_OaepSha256_B64,
            ReadOnlySpan<byte>.Empty,
            [1],
            ReadOnlySpan<byte>.Empty));

        Assert.Throws<NotSupportedException>(() => WriteEncString(rsaValue));
    }

    private static EncString DecodePacked(
        EncryptionType type,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> mac,
        byte[] key)
    {
        EncString value = EncString.FromBytes(CryptoTestFixtures.Pack(type, iv, data, mac));
        _ = value.Decode(key);
        return value;
    }

    [Fact]
    public void Encoded_segment_decoding_rejects_an_undersized_iv_buffer()
    {
        Assert.Throws<FormatException>(DecodeWithShortIvBuffer);
    }

    private static void DecodeWithShortIvBuffer()
    {
        byte[] encoded = System.Text.Encoding.UTF8.GetBytes(CryptoTestFixtures.AuthenticatedEncString);
        var layout = EncString.ParseEncodedLayoutOrThrow(encoded);
        EncString.DecodeEncodedSegments(
            encoded, in layout, new byte[layout.DataLength], new byte[layout.IvLength - 1], new byte[layout.MacLength]);
    }

    private static EncString ReadEncStringJson(string json)
    {
        Utf8JsonReader reader = new(System.Text.Encoding.UTF8.GetBytes(json));
        Assert.True(reader.Read());
        return new EncStringJsonConverter().Read(ref reader, typeof(EncString), new JsonSerializerOptions());
    }

    private static string WriteEncString(EncString value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            new EncStringJsonConverter().Write(writer, value, new JsonSerializerOptions());
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string ToBase64Url(string value) => value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
