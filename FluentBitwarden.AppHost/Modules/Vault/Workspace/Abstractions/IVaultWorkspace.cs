using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

/// <summary>
/// Stateless facade over the vault data pipeline: loading the decrypted vault from the local
/// cache, synchronizing it with the server and saving ciphers. Shields consumers outside the
/// Vault module from its internal classes.
/// </summary>
internal interface IVaultWorkspace
{
    LoadedVaultData Load(UserKey userKey, KeySession keys);

    Task<VaultSyncResult> SyncAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        bool force,
        CancellationToken cancellationToken);

    Task<VaultCipher> SaveCipherAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        VaultCipher cipher,
        CancellationToken cancellationToken);
}
