namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IVaultWorkspace
{
    bool IsOpen { get; }
    UserId OpenedForUserId { get; }

    void Open(DecryptedUserKey userKey);
    void Reload(DecryptedUserKey userKey);
    void Close();
}
