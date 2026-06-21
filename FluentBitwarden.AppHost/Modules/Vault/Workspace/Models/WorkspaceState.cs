namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

internal sealed record WorkspaceState(UserId UserId, LoadedVaultData Data)
{
    public static readonly WorkspaceState Empty = new(UserId.Empty, new LoadedVaultData([], [], [], []));
}