using MemoryPack;

namespace BitwardenApi.Infrastructure.Serialization;

internal sealed class TotpValueFormatter : MemoryPackFormatter<TotpValue>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TotpValue? value)
    {
        if (value is null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        writer.WritePackable(value.StateObj);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref TotpValue? value)
    {
        if (reader.PeekIsNull())
        {
            reader.Advance(1);
            value = null;
            return;
        }

        TotpValue.State? state = reader.ReadPackable<TotpValue.State>();
        if (state is null)
        {
            value = null;
            return;
        }

        value = new TotpValue(state);
    }
}

public sealed class TotpValueFormatterAttribute : MemoryPackCustomFormatterAttribute<TotpValue>
{
    public override IMemoryPackFormatter<TotpValue> GetFormatter() => new TotpValueFormatter();
}