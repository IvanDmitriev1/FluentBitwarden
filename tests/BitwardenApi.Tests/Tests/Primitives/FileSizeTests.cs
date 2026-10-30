using System.Globalization;

using BitwardenApi.Primitives;
using BitwardenApi.Vault.Internal;

namespace BitwardenApi.Tests.Tests.Primitives;

public sealed class FileSizeTests
{
    [Fact]
    public void Stores_zero_and_the_largest_nonnegative_byte_count()
    {
        Assert.Equal(0, FileSize.Zero.Bytes);
        Assert.Equal(long.MaxValue, FileSize.FromBytes(long.MaxValue).Bytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => FileSize.FromBytes(-1));
    }

    [Fact]
    public void ToString_uses_invariant_decimal_digits()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal("1234567", FileSize.FromBytes(1_234_567).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("123", 123)]
    [InlineData("\"456\"", 456)]
    [InlineData("9223372036854775807", long.MaxValue)]
    public void Json_accepts_nonnegative_Int64_numbers_and_decimal_strings(string json, long expectedBytes)
    {
        FileSize result = JsonSerializer.Deserialize(json, VaultJsonContext.ConfiguredDefault.FileSize);

        Assert.Equal(expectedBytes, result.Bytes);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("9223372036854775808")]
    [InlineData("\"-1\"")]
    [InlineData("\"1.5\"")]
    [InlineData("true")]
    [InlineData("null")]
    public void Json_rejects_negative_nonintegral_out_of_range_and_non_numeric_values(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, VaultJsonContext.ConfiguredDefault.FileSize));
    }

    [Fact]
    public void Json_writes_file_size_as_a_number()
    {
        string json = JsonSerializer.Serialize(FileSize.FromBytes(42), VaultJsonContext.ConfiguredDefault.FileSize);

        Assert.Equal("42", json);
    }
}
