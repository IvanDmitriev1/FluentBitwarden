using System.Buffers.Binary;
using System.Security.Cryptography;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class WebAuthnAssertionTests
{
    [Theory]
    [InlineData(0U)]
    [InlineData(1U)]
    [InlineData(2147483648U)]
    [InlineData(uint.MaxValue)]
    public void Create_writes_authenticator_data_and_signs_the_client_binding(uint signCount)
    {
        byte[] rpIdHash = Enumerable.Range(0, 32).Select(static value => (byte)value).ToArray();
        byte[] clientDataHash = Enumerable.Range(32, 32).Select(static value => (byte)value).ToArray();
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = signingKey.ExportPkcs8PrivateKey();

        try
        {
            (byte[] authenticatorData, byte[] signature) = WebAuthnAssertion.Create(
                rpIdHash,
                clientDataHash,
                privateKey,
                signCount);

            Assert.Equal(37, authenticatorData.Length);
            Assert.Equal(rpIdHash, authenticatorData.AsSpan(0, 32).ToArray());
            Assert.Equal((byte)0x1D, authenticatorData[32]);
            Assert.Equal(signCount, BinaryPrimitives.ReadUInt32BigEndian(authenticatorData.AsSpan(33)));

            Span<byte> signedData = stackalloc byte[69];
            authenticatorData.CopyTo(signedData);
            clientDataHash.CopyTo(signedData[37..]);
            byte[] signedHash = SHA256.HashData(signedData);
            Assert.True(signingKey.VerifyHash(signedHash, signature, DSASignatureFormat.Rfc3279DerSequence));

            signedData[37] ^= 0x80;
            Assert.False(signingKey.VerifyHash(
                SHA256.HashData(signedData),
                signature,
                DSASignatureFormat.Rfc3279DerSequence));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    [Theory]
    [InlineData(31, 32)]
    [InlineData(33, 32)]
    [InlineData(32, 31)]
    [InlineData(32, 33)]
    public void Create_rejects_hashes_that_are_not_sha256_sized(int rpIdHashLength, int clientDataHashLength)
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = signingKey.ExportPkcs8PrivateKey();

        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WebAuthnAssertion.Create(
                new byte[rpIdHashLength],
                new byte[clientDataHashLength],
                privateKey,
                0));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    [Fact]
    public void Create_rejects_malformed_private_key_material()
    {
        Assert.Throws<CryptographicException>(() => WebAuthnAssertion.Create(
            new byte[32],
            new byte[32],
            [0x30, 0x01, 0x00],
            0));
    }

    [Fact]
    public void Create_rejects_a_private_key_with_trailing_bytes()
    {
        byte[] rpIdHash = new byte[32];
        byte[] clientDataHash = new byte[32];
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = signingKey.ExportPkcs8PrivateKey();
        byte[] trailingBytes = [.. privateKey, 0xA5];

        try
        {
            Assert.Throws<CryptographicException>(() => WebAuthnAssertion.Create(
                rpIdHash,
                clientDataHash,
                trailingBytes,
                0));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
            CryptographicOperations.ZeroMemory(trailingBytes);
        }
    }
}
