namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultReaderRepository
{
    public delegate void CipherVisitor<in TState>(
        TState state,
        ref readonly VaultCipherDto dto,
        ReadOnlySpan<byte> payload);

    void ReadAllCiphers<TState>(UserId userId, TState stateObj, CipherVisitor<TState> onCipher);

    IEnumerable<VaultFolderDto> GetAllFolders(UserId userId);
    IEnumerable<VaultCollectionDto> GetAllCollections(UserId userId);
}
