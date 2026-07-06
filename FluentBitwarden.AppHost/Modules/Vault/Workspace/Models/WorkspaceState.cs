using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

internal sealed record WorkspaceState(UserKey? UserKey, LoadedVaultData Data)
{
    public static readonly WorkspaceState Empty = new(null, new LoadedVaultData([], [], [], []));
}