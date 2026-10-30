using MemoryPack;

using BitwardenApi.Vault.Ciphers.Models;
namespace BitwardenApi.Vault.Ciphers.Serialization;
public sealed class TotpValueFormatterAttribute : MemoryPackCustomFormatterAttribute<TotpValue>
{
    public override IMemoryPackFormatter<TotpValue> GetFormatter() => new TotpValueFormatter();
}