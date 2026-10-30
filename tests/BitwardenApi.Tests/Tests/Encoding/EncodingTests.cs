using TextEncoding = System.Text.Encoding;

using BitwardenApi.Encoding;

namespace BitwardenApi.Tests.Tests.Encoding;

public sealed class EncodingTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("MY======", "66")]
    [InlineData("MZXQ====", "666F")]
    [InlineData("MZXW6===", "666F6F")]
    [InlineData("MZXW6YQ=", "666F6F62")]
    [InlineData("MZXW6YTB", "666F6F6261")]
    [InlineData("MZXW6YTBOI======", "666F6F626172")]
    [InlineData("my", "66")]
    public void Base32_decodes_RFC_4648_values(string text, string expectedHex)
    {
        byte[] source = TextEncoding.ASCII.GetBytes(text);
        byte[] destination = new byte[Base32.GetMaxDecodedLength(source.Length)];

        bool success = Base32.TryDecode(source, destination, out int bytesWritten);

        Assert.True(success);
        Assert.Equal(Convert.FromHexString(expectedHex), destination.AsSpan(0, bytesWritten).ToArray());
    }

    [Theory]
    [InlineData("!")]
    [InlineData("MY======A")]
    [InlineData("M")]
    [InlineData("MZX")]
    [InlineData("MZXW6Y")]
    [InlineData("MY=")]
    [InlineData("MZ")]
    public void Base32_rejects_invalid_alphabet_length_padding_and_residual_bits(string text)
    {
        byte[] destination = new byte[Base32.GetMaxDecodedLength(text.Length)];

        bool success = Base32.TryDecode(TextEncoding.ASCII.GetBytes(text), destination, out int bytesWritten);

        Assert.False(success);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void Base32_resets_bytes_written_when_destination_is_too_small()
    {
        bool success = Base32.TryDecode("MY"u8, Span<byte>.Empty, out int bytesWritten);

        Assert.False(success);
        Assert.Equal(0, bytesWritten);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(8, 5)]
    [InlineData(16, 10)]
    public void Base32_reports_safe_decoded_capacity(int encodedLength, int expectedLength)
    {
        Assert.Equal(expectedLength, Base32.GetMaxDecodedLength(encodedLength));
    }

    [Fact]
    public void Base32_rejects_negative_and_overflowing_capacity_requests()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base32.GetMaxDecodedLength(-1));
        Assert.Throws<OverflowException>(() => Base32.GetMaxDecodedLength(int.MaxValue));
    }

    [Theory]
    [InlineData("AQID", "010203")]
    [InlineData("", "")]
    public void Base64_decodes_valid_text(string text, string expectedHex)
    {
        bool success = Base64Extensions.TryConvertFromBase64Chars(text, out byte[] bytes);

        Assert.True(success);
        Assert.Equal(Convert.FromHexString(expectedHex), bytes);
    }

    [Fact]
    public void Base64_returns_an_empty_array_for_invalid_text()
    {
        bool success = Base64Extensions.TryConvertFromBase64Chars("AQID!", out byte[] bytes);

        Assert.False(success);
        Assert.Empty(bytes);
    }

    [Fact]
    public void RemoveAsciiWhitespaceInPlace_keeps_other_bytes_and_compacts_the_span()
    {
        byte[] buffer = [(byte)'A', (byte)' ', (byte)'\t', 0xA0, (byte)'\r', (byte)'\n', (byte)'B'];

        int bytesWritten = buffer.AsSpan().RemoveAsciiWhitespaceInPlace();

        Assert.Equal(3, bytesWritten);
        Assert.Equal(new byte[] { (byte)'A', 0xA0, (byte)'B' }, buffer.AsSpan(0, bytesWritten).ToArray());
    }

    [Fact]
    public void RemoveAsciiWhitespaceInPlace_accepts_an_empty_span()
    {
        Assert.Equal(0, Span<byte>.Empty.RemoveAsciiWhitespaceInPlace());
    }
}
