using MemoryPack;

namespace BitwardenApi.Common.MemoryPackFormatters;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StronglyTypedIdFormatterAttribute<T>
    : MemoryPackCustomFormatterAttribute<StronglyTypedIdMemoryPackFormatter<T>, T>
    where T : struct, ISpanParsable<T>
{
    public override StronglyTypedIdMemoryPackFormatter<T> GetFormatter() => new();
}