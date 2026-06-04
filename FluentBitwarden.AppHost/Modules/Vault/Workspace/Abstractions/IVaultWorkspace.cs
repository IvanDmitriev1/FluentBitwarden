namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IVaultWorkspace
{
    bool IsOpen { get; }
    UserId OpenedForUserId { get; }

    ValueTask OpenAsync(DecryptedUserKey userKey, CancellationToken cancellationToken);
    void Reload(DecryptedUserKey userKey);
    void Close();
}
