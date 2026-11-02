namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultCipherService
{
    Task<Cipher> SaveAsync(Cipher cipher, CancellationToken cancellationToken = default);
}
