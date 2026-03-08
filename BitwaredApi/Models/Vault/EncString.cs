namespace BitwaredApi.Models.Vault;

public sealed record EncString(string Value)
{
    public override string ToString() => Value;
}
