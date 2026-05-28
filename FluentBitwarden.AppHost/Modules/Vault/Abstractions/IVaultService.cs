using BitwardenApi.Models;
using FluentBitwarden.Contracts.Vault.Models;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultService
{
    event EventHandler<IVaultService, VaultChangedEventArgs> VaultChanged;

    void LoadLocalVault();
    Task<VaultSyncResult> SyncVaultAsync(CancellationToken token);

    VaultCipher? GetCipher(CipherId id);
    VaultCipher[] GetCiphers(VaultCipherQuery query);

    List<Fido2Credential> GetFido2Credentials(string rpId);
    List<SshPublicIdentityResponce> GetAvailableSshKeys();
    SshKeyVaultCipher? GetSsh(ReadOnlyMemory<byte> publicKeyBlob);

    VaultFolder[] GetFolders();
    VaultCollection[] GetCollections();
}