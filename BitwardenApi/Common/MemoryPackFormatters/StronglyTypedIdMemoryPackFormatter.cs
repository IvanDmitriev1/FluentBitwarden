using MemoryPack;

namespace BitwardenApi.Common.MemoryPackFormatters;

public sealed class StronglyTypedIdMemoryPackFormatter<T> : MemoryPackFormatter<T>
    where T : struct, ISpanParsable<T>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
    {
        writer.WriteUtf16(value.ToString());
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref T value)
    {
        string? text = reader.ReadString();

        value = text is null
            ? default
            : T.Parse(text, provider: null);
    }
}