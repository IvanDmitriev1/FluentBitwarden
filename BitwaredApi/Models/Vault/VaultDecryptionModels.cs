namespace BitwaredApi.Models.Vault;

public abstract record VaultDecryptionOutcome<T>
{
    private VaultDecryptionOutcome() { }

    public sealed record Success(T Value) : VaultDecryptionOutcome<T>;
    public sealed record Failed(string Message) : VaultDecryptionOutcome<T>;
}
