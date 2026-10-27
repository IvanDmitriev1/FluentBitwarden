using OtpNet;

using TextEncoding = System.Text.Encoding;

namespace BitwardenApi.Tests.Tests.Vault.Ciphers;

public sealed class CipherValueTests
{
    [Fact]
    public void Bare_totp_secret_parses_ascii_whitespace_and_uses_default_parameters()
    {
        TotpValue value = Parse(" GEZ\tDGNBVGY3TQOJQ\r\n");

        Assert.Equal(OtpType.Totp, value.StateObj.Type);
        Assert.Equal(OtpHashMode.Sha1, value.StateObj.HashMode);
        Assert.Equal(TextEncoding.ASCII.GetBytes("1234567890"), value.StateObj.SecretBytes);
        Assert.Equal(6, value.StateObj.Digits);
        Assert.Equal(30, value.Step);
        Assert.Equal("GEZDGNBVGY3TQOJQ", value.ToStorageString());
    }

    [Fact]
    public void Totp_url_parses_case_insensitive_parameters_and_generates_storage_value()
    {
        TotpValue value = Parse(
            "otpauth://tOtP/account:alice?ignored&ISSUER=Example&SECRET=GEZDGNBVGY3TQOJQ&ALGORITHM=SHA256&DIGITS=8&PERIOD=45");

        Assert.Equal(OtpType.Totp, value.StateObj.Type);
        Assert.Equal(OtpHashMode.Sha256, value.StateObj.HashMode);
        Assert.Equal(8, value.StateObj.Digits);
        Assert.Equal(45, value.StateObj.PeriodSeconds);
        Assert.Equal(45, value.Step);
        Assert.Equal(
            "otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA256&digits=8&period=45",
            value.ToStorageString());
        Assert.Matches("^[0-9]{8}$", value.ComputeTotp());
    }

    [Fact]
    public void Hotp_sha512_storage_uses_the_nondefault_wire_form()
    {
        TotpValue value = Parse(
            "otpauth://hotp/label?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA512&digits=7&period=31");

        Assert.Equal(OtpType.Hotp, value.StateObj.Type);
        Assert.Equal(OtpHashMode.Sha512, value.StateObj.HashMode);
        Assert.Equal(7, value.StateObj.Digits);
        Assert.Equal(31, value.Step);
        Assert.Equal(
            "otpauth://hotp/?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA512&digits=7&period=31",
            value.ToStorageString());
        Assert.Matches("^[0-9]{7}$", value.ComputeTotp());
    }

    [Fact]
    public void Missing_or_invalid_numeric_url_parameters_keep_the_defaults()
    {
        TotpValue value = Parse("otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&digits=bad&period=bad");

        Assert.Equal(6, value.StateObj.Digits);
        Assert.Equal(30, value.StateObj.PeriodSeconds);
        Assert.Equal("GEZDGNBVGY3TQOJQ", value.ToStorageString());
    }

    [Fact]
    public void Totp_url_accepts_a_one_second_period()
    {
        TotpValue value = Parse("otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&period=1");

        Assert.Equal(1, value.StateObj.PeriodSeconds);
        Assert.Equal(1, value.Step);
        Assert.Equal(
            "otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA1&digits=6&period=1",
            value.ToStorageString());
    }

    [Fact]
    public void Totp_with_nondefault_digits_keeps_sha1_when_rendered_as_a_url()
    {
        TotpValue value = Parse("otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&digits=8");

        Assert.Equal(
            "otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA1&digits=8&period=30",
            value.ToStorageString());
    }

    [Fact]
    public void Expiration_is_the_next_step_boundary()
    {
        TotpValue value = Parse("GEZDGNBVGY3TQOJQ");
        DateTimeOffset before = DateTimeOffset.UtcNow;

        DateTimeOffset expiresAt = value.ExpiresAt;
        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.True(expiresAt > before);
        Assert.True(expiresAt <= after.AddSeconds(value.Step));
        Assert.Equal(0, expiresAt.ToUnixTimeSeconds() % value.Step);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("MZ")]
    [InlineData("MZX")]
    [InlineData("MZXW6Y")]
    [InlineData("not$base32")]
    [InlineData("otpauth://")]
    [InlineData("otpauth://totp")]
    [InlineData("otpauth://unknown/?secret=GEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp/?algorithm=SHA256")]
    [InlineData("OTPAUTH://totp/?secret=GEZDGNBVGY3TQOJQ")]
    public void TryParse_rejects_empty_or_invalid_secrets_and_urls(string text)
    {
        bool success = TotpValue.TryParse(TextEncoding.UTF8.GetBytes(text), out TotpValue? value);

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void Totp_url_without_a_query_or_secret_is_rejected()
    {
        bool success = TotpValue.TryParse("otpauth://totp/account"u8.ToArray(), out TotpValue? value);

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void Fido2_wire_values_round_trip_case_insensitively()
    {
        Assert.Equal("public-key", Fido2CredentialKeyType.PublicKey.ToWireValue());
        Assert.Equal("ECDSA", Fido2CredentialKeyAlgorithm.Ecdsa.ToWireValue());
        Assert.Equal("P-256", Fido2CredentialKeyCurve.P256.ToWireValue());

        Assert.Equal(Fido2CredentialKeyType.PublicKey, Fido2CredentialEnumExtensions.ParseKeyType("PUBLIC-KEY"u8));
        Assert.Equal(Fido2CredentialKeyAlgorithm.Ecdsa, Fido2CredentialEnumExtensions.ParseKeyAlgorithm("ecdsa"u8));
        Assert.Equal(Fido2CredentialKeyCurve.P256, Fido2CredentialEnumExtensions.ParseKeyCurve("p-256"u8));
    }

    [Fact]
    public void Fido2_wire_mappings_reject_unsupported_enum_values_and_text()
    {
        Assert.Throws<NotSupportedException>(() => ((Fido2CredentialKeyType)0).ToWireValue());
        Assert.Throws<NotSupportedException>(() => ((Fido2CredentialKeyAlgorithm)0).ToWireValue());
        Assert.Throws<NotSupportedException>(() => ((Fido2CredentialKeyCurve)0).ToWireValue());

        Assert.Throws<NotSupportedException>(() => Fido2CredentialEnumExtensions.ParseKeyType("publickey"u8));
        Assert.Throws<NotSupportedException>(() => Fido2CredentialEnumExtensions.ParseKeyAlgorithm("RSA"u8));
        Assert.Throws<NotSupportedException>(() => Fido2CredentialEnumExtensions.ParseKeyCurve("P-384"u8));
    }

    private static TotpValue Parse(string text)
    {
        bool success = TotpValue.TryParse(TextEncoding.UTF8.GetBytes(text), out TotpValue? value);
        Assert.True(success);
        return Assert.IsType<TotpValue>(value);
    }
}
