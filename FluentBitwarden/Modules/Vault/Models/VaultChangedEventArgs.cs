using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Models;

public sealed class VaultChangedEventArgs : EventArgs
{
    public enum VaultChangeKind { FullReload, CipherAdded, CipherUpdated, CipherDeleted, FoldersReloaded }

    public required VaultChangeKind Kind { get; init; }
    public required CipherId CipherId { get; init; }
}