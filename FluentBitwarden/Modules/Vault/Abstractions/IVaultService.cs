using BitwardenApi.Models;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultService
{
    event EventHandler<IVaultService, VaultChangedEventArgs> VaultChanged;

    void LoadLocalVault();
    Task<VaultSyncResult> SyncVaultAsync(CancellationToken token);

    VaultCipher? GetCipher(CipherId id);
    List<VaultCipher> GetCiphers(CipherQuery query);

    List<Fido2Credential> GetFido2Credentials(string rpId);
    List<SshPublicIdentityResponce> GetAvailableSshKeys();
    SshKeyVaultCipher? GetSsh(ReadOnlyMemory<byte> publicKeyBlob);

    List<VaultFolder> GetFolders();
    List<VaultCollection> GetCollections();
}