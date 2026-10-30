using System.Security.Cryptography;

using BitwardenApi.Tests.Infrastructure;
using BitwardenApi.Vault.Attachments;

namespace BitwardenApi.Tests.Tests.Vault.Attachments;

public sealed class AttachmentDecryptorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(40959)]
    [InlineData(40960)]
    [InlineData(40961)]
    public async Task Decrypts_authenticated_attachment_bytes_at_copy_buffer_boundaries(int plaintextLength)
    {
        using var fixture = new AttachmentCryptoFixture();
        byte[] expected = CreatePayload(plaintextLength);
        byte[] encrypted = fixture.EncryptAttachment(expected);
        using var source = new MemoryStream(encrypted);
        using var plaintext = new MemoryStream();

        await AttachmentDecryptor.DecryptToAsync(
            fixture.Key,
            source,
            plaintext,
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, plaintext.ToArray());
        CryptographicOperations.ZeroMemory(expected);
    }

    [Fact]
    public async Task Rejects_unsupported_encryption_type_without_writing_plaintext()
    {
        using var fixture = new AttachmentCryptoFixture();
        byte[] encrypted = fixture.EncryptAttachment("private attachment"u8.ToArray());
        encrypted[0] = (byte)EncryptionType.AesCbc256_B64;
        using var source = new MemoryStream(encrypted);
        using var plaintext = new MemoryStream();

        await Assert.ThrowsAsync<CryptographicException>(() => AttachmentDecryptor.DecryptToAsync(
            fixture.Key,
            source,
            plaintext,
            TestContext.Current.CancellationToken));

        Assert.Empty(plaintext.ToArray());
    }

    [Fact]
    public async Task Rejects_truncated_header_before_writing_plaintext()
    {
        using var fixture = new AttachmentCryptoFixture();
        byte[] truncatedHeader = new byte[48];
        truncatedHeader[0] = (byte)EncryptionType.AesCbc256_HmacSha256_B64;
        using var source = new MemoryStream(truncatedHeader);
        using var plaintext = new MemoryStream();

        await Assert.ThrowsAsync<EndOfStreamException>(() => AttachmentDecryptor.DecryptToAsync(
            fixture.Key,
            source,
            plaintext,
            TestContext.Current.CancellationToken));

        Assert.Empty(plaintext.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rejects_tampered_or_truncated_authenticated_body_without_releasing_plaintext(bool truncate)
    {
        using var fixture = new AttachmentCryptoFixture();
        byte[] encrypted = fixture.EncryptAttachment("private attachment"u8.ToArray());
        if (truncate)
            encrypted = encrypted[..^1];
        else
            encrypted[17] ^= 1;

        using var source = new MemoryStream(encrypted);
        using var plaintext = new MemoryStream();

        await Assert.ThrowsAsync<CryptographicException>(() => AttachmentDecryptor.DecryptToAsync(
            fixture.Key,
            source,
            plaintext,
            TestContext.Current.CancellationToken));

        Assert.Empty(plaintext.ToArray());
    }

    [Fact]
    public async Task Cancellation_while_reading_attachment_header_produces_no_plaintext()
    {
        using var fixture = new AttachmentCryptoFixture();
        using var cancellation = new CancellationTokenSource();
        using var source = new AttachmentBlockingReadStream();
        using var plaintext = new MemoryStream();
        Task decryption = AttachmentDecryptor.DecryptToAsync(fixture.Key, source, plaintext, cancellation.Token);

        await source.ReadStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => decryption);
        Assert.Empty(plaintext.ToArray());
    }

    private static byte[] CreatePayload(int length)
        => [.. Enumerable.Range(0, length).Select(static value => (byte)(value * 31))];
}
