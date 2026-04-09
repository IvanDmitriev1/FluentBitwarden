namespace FluentBitwarden.Modules.Vault.Models;

public enum VaultChangeKind { FullReload, CipherAdded, CipherUpdated, CipherDeleted, FoldersReloaded }

public sealed class VaultChangedEventArgs(VaultChangeKind kind, Guid? itemId = null) : EventArgs
{
    public VaultChangeKind Kind { get; } = kind;
    public Guid? ItemId { get; } = itemId;
}