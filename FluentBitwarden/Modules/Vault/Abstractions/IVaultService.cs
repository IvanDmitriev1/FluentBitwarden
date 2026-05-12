using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultService
{
    event EventHandler<IVaultService, VaultChangedEventArgs> VaultChanged;

    void LoadLocalVault();
    Task<VaultSyncResult> SyncVaultAsync(CancellationToken token);

    IReadOnlyList<VaultCipher> GetCiphers();
    VaultCipher? GetCipher(CipherId id);
    IReadOnlyList<VaultCipher> Search(CipherQuery query);

    IReadOnlyList<Fido2Credential> GetFido2Credentials(string rpId);
    IReadOnlyList<SshPublicIdentityResponce> GetAvailableSshKeys();
    SshKeyVaultCipher? GetSsh(ReadOnlyMemory<byte> publicKeyBlob);

    IReadOnlyList<VaultFolder> GetFolders();
    IReadOnlyList<VaultCollection> GetCollections();
}