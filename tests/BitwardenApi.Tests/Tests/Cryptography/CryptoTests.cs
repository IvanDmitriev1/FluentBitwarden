using System.Security.Cryptography;
using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests;

public sealed class CryptoTests
{
    [Fact]
    public void Hash_master_password_normalizes_email_before_deriving_the_authentication_hash()
    {
        MasterPasswordHash hash = MasterPassword.HashMasterPassword(
            "  USER@EXAMPLE.TEST  ",
            "correct horse battery staple",
            new KdfConfig.Pbkdf2(2));

        // Python hashlib.pbkdf2_hmac("sha256", …, 2, 32), then one PBKDF2 round for the auth hash.
        Assert.Equal("e5neV78YzoV9hBF8Y8ozLm3COom6tUb9PfeHGweYD6E=", hash.Value);
    }

    [Fact]
    public void Hash_master_password_with_pbkdf2_matches_the_fixed_reference_vector()
    {
        MasterPasswordHash hash = MasterPassword.HashMasterPassword(
            "user@example.test",
            "password",
            new KdfConfig.Pbkdf2(2));

        // Python hashlib.pbkdf2_hmac("sha256", b"password", b"user@example.test", 2, 32), then auth PBKDF2(1).
        Assert.Equal("mrEkdfLXzlATl6Z6iTHwBgSsWzgciEl5qHNHkg2axaI=", hash.Value);
        Assert.Equal(hash.Value, hash.ToString());
    }

    [Fact]
    public void Hash_master_password_with_argon2id_matches_the_fixed_reference_vector()
    {
        // The SDK hashes the UTF-8 salt with SHA-256 before Argon2id.
        MasterPasswordHash hash = MasterPassword.HashMasterPassword(
            "user@example.test",
            "password",
            new KdfConfig.Argon2Id(2, 8, 1));

        Assert.Equal("XuB6Ndmbr8qe1Wrdyo8MyZljVe4Uh8hhBAj+AAVX8dg=", hash.Value);
    }

    [Fact]
    public void Stretch_expands_the_master_key_into_the_expected_encryption_and_mac_material()
    {
        using MasterKey masterKey = MasterKey.Derive(
            "password",
            "user@example.test",
            new KdfConfig.Pbkdf2(2));
        using StretchedMasterKey stretchedKey = masterKey.Stretch();

        // Independently calculated with Python hashlib.pbkdf2_hmac, then HMAC-SHA256(PRK, "enc"/"mac" + 0x01).
        Assert.Equal("084940AB1E126A3076FE05692CC9546589E79007774C25505F639456D605D29FF8E279E1613F9020128E7937C5E9B8F6E960E21C9F02C8DEAC83E265AA61BCEA", Convert.ToHexString(stretchedKey.Span));
    }

    [Fact]
    public void Pbkdf2_char_and_utf8_byte_inputs_match_the_same_fixed_reference_vector()
    {
        byte[] charPasswordOutput = new byte[32];
        byte[] bytePasswordOutput = new byte[32];

        Pbkdf2Kdf.Derive("password", "salt", 1, charPasswordOutput);
        Pbkdf2Kdf.Derive(System.Text.Encoding.UTF8.GetBytes("password"), "salt", 1, bytePasswordOutput);

        const string expected = "120FB6CFFCF8B32C43E7225256C4F837A86548C92CCC35480805987CB70BE17B";
        Assert.Equal(expected, Convert.ToHexString(charPasswordOutput));
        Assert.Equal(expected, Convert.ToHexString(bytePasswordOutput));
    }

    [Fact]
    public void Hkdf_expand_matches_rfc_5869_test_case_three()
    {
        byte[] pseudorandomKey = Convert.FromHexString("19EF24A32C717B167F33A91D6F648BDF96596776AFDB6377AC434C1C293CCB04");
        Span<byte> output = stackalloc byte[42];

        Hkdf.Expand(pseudorandomKey, "", output);

        Assert.Equal(
            "8DA4E775A563C18F715F802A063C5A31B8A11F5C5EE1879EC3454E5F3C738D2D9D201395FAA4B61A96C8",
            Convert.ToHexString(output));
    }

    [Fact]
    public void Master_key_rejects_an_unknown_kdf_configuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MasterKey.Derive("password", "salt", new UnknownKdf()));
    }

    [Fact]
    public void Master_key_rejects_zero_pbkdf2_iterations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MasterKey.Derive("password", "salt", new KdfConfig.Pbkdf2(0)));
    }

    [Fact]
    public void Disposing_a_stretched_key_zeros_its_borrowed_material()
    {
        using MasterKey masterKey = MasterKey.Derive("password", "salt", new KdfConfig.Pbkdf2(1));
        StretchedMasterKey stretchedKey = masterKey.Stretch();
        ReadOnlySpan<byte> borrowedMaterial = stretchedKey.Span;

        stretchedKey.Dispose();
        stretchedKey.Dispose();

        Assert.Equal(new byte[64], borrowedMaterial.ToArray());
    }

    [Fact]
    public void Decode_decrypts_an_authenticated_fixed_enc_string_fixture()
    {
        EncString value = TestApiSupport.ParseEncString("2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=");

        Assert.Equal("fixture plaintext", value.Decode(TestKey));
    }

    [Fact]
    public void Decode_rejects_a_tampered_mac()
    {
        EncString value = TestApiSupport.ParseEncString("2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284A=");

        Assert.Throws<CryptographicException>(() => value.Decode(TestKey));
    }

    [Fact]
    public void Decode_rejects_a_downgraded_authenticated_enc_string_on_every_decode_path()
    {
        const string encoded = "0.NQfjHLr6za7VQVAbrpL81w==|wfrjmyJ0bfwkQlySrhw8dA==";
        byte[] key = Convert.FromBase64String(
            "hvBMMb1t79YssFZkpetYsM3deyVuQv4r88Uj9gvYe0+G8EwxvW3v1iywVmSl61iwzd17JW5C/ivzxSP2C9h7Tw==");
        EncString value = TestApiSupport.ParseEncString(encoded);

        Assert.Throws<CryptographicException>(() => value.Decode(key));
        Assert.Throws<CryptographicException>(() => value.DecodeToArray(key));
        Assert.Throws<CryptographicException>(() => System.Text.Encoding.UTF8.GetBytes(encoded).AsSpan().DecodeEncString(key));
        Assert.Throws<CryptographicException>(() => System.Text.Encoding.UTF8.GetBytes(encoded).AsSpan().DecodeEncStringInPlace(key));
    }

    [Theory]
    [InlineData(31, false)]
    [InlineData(32, true)]
    [InlineData(33, false)]
    public void Legacy_enc_string_matches_sdk_vector_and_requires_exactly_32_key_bytes(int keyLength, bool succeeds)
    {
        byte[] key = Convert.FromBase64String(
            "hvBMMb1t79YssFZkpetYsM3deyVuQv4r88Uj9gvYe08=");
        Array.Resize(ref key, keyLength);
        // Pinned SDK master_key.rs test_decrypt_cbc256 interoperability vector.
        EncString value = TestApiSupport.ParseEncString(
            "0.tn/heK4HLbbEe+yEkC+kvw==|8QM94f7aVTtjm/bmvRdVxOxiLiiZtHYYO7+oBdjFCkilncesx0iVrXPl+tMKqW+Jo7+FtZdPNsTrL6RdoG7i5QbCRVwK+9010+xm7MTQY8s=");

        if (succeeds)
        {
            Assert.Equal(
                "74AABB2B50D4C1CAEAB5394297F93B2F46103904AA4E55F198E85B390957D1F528F56A8C02E18AD562DF5CA887D016C21F15B2FCCBC623AE35DAFE97EB390762",
                Convert.ToHexString(value.DecodeToArray(key)));
        }
        else
        {
            Assert.Throws<CryptographicException>(() => value.DecodeToArray(key));
        }
    }

    [Theory]
    [InlineData(63, false)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void Authenticated_enc_string_requires_exactly_64_key_bytes(int keyLength, bool succeeds)
    {
        byte[] key = TestKey.ToArray();
        Array.Resize(ref key, keyLength);
        EncString value = TestApiSupport.ParseEncString(CryptoTestFixtures.AuthenticatedEncString);

        if (succeeds)
        {
            Assert.Equal("fixture plaintext", value.Decode(key));
            EncString encrypted = EncString.Encrypt("key length", key);
            Assert.Equal("key length", encrypted.Decode(key));
        }
        else
        {
            Assert.Throws<CryptographicException>(() => value.Decode(key));
            Assert.Throws<CryptographicException>(() => EncString.Encrypt("key length", key));
        }
    }

    [Theory]
    [InlineData("2.invalid|AQ==|AQ==")]
    [InlineData("2.AQ==|AQ==")]
    public void Decode_rejects_malformed_enc_string_input(string encoded)
    {
        Assert.Throws<FormatException>(() => TestApiSupport.ParseEncString(encoded));
    }

    [Fact]
    public void Decode_rejects_an_unsupported_encryption_type()
    {
        EncString value = TestApiSupport.ParseEncString("3.AQ==");

        Assert.Throws<CryptographicException>(() => value.Decode(TestKey));
        Assert.Throws<CryptographicException>(() => value.DecodeToArray(TestKey));
    }

    [Fact]
    public void Encrypt_round_trips_unicode_plaintext_as_a_supplemental_check()
    {
        EncString encrypted = EncString.Encrypt("secret \u2713", TestKey);

        Assert.Equal("secret \u2713", encrypted.Decode(TestKey));
    }

    private static readonly byte[] TestKey = [.. Enumerable.Range(0, 64).Select(static value => (byte)value)];

    private sealed record UnknownKdf : KdfConfig;

}
