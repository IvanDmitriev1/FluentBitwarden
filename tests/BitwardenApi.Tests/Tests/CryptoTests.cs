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
    }

    [Fact]
    public void Hash_master_password_with_argon2id_matches_the_fixed_reference_vector()
    {
        MasterPasswordHash hash = MasterPassword.HashMasterPassword(
            "user@example.test",
            "password",
            new KdfConfig.Argon2Id(2, 8, 1));

        // argon2-cffi 25.1.0: Argon2id(password, email, time=2, memory=8192 KiB, parallelism=1), then auth PBKDF2(1).
        Assert.Equal("NyKRRcjEiUThbYJBpNForKoV5XIyxzrX1I13CJ9h/X4=", hash.Value);
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
    }

    [Fact]
    public void Encrypt_round_trips_unicode_plaintext_as_a_supplemental_check()
    {
        EncString encrypted = EncString.Encrypt("secret \u2713", TestKey);

        Assert.Equal("secret \u2713", encrypted.Decode(TestKey));
    }

    private static readonly byte[] TestKey = [.. Enumerable.Range(0, 64).Select(static value => (byte)value)];

}
