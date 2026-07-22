using System.Globalization;
using System.Text.Json.Serialization;
using MemoryPack;

namespace BitwardenApi.Primitives;

[JsonConverter(typeof(FileSizeJsonConverter))]
[MemoryPackable]
public readonly partial record struct FileSize
{
    public FileSize(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "File size must not be negative.");

        Bytes = bytes;
    }

    public long Bytes { get; }

    public static FileSize Zero { get; } = new(0);
    public static FileSize FromBytes(long bytes) => new(bytes);
    public override string ToString() => Bytes.ToString(CultureInfo.InvariantCulture);
}

internal sealed class FileSizeJsonConverter : JsonConverter<FileSize>
{
    public override FileSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long bytes = reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt64(out long value) => value,
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out long value) => value,
            _ => throw new JsonException("File size must be a non-negative Int64 value.")
        };

        if (bytes < 0)
            throw new JsonException("File size must be a non-negative Int64 value.");

        return FileSize.FromBytes(bytes);
    }

    public override void Write(Utf8JsonWriter writer, FileSize value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Bytes);
    }
}
