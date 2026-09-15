using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Attachments;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Ipc;

/// <summary>
/// In-process <see cref="IVaultClient"/> over the unlocked session. Reads resolve the session once
/// and work against its immutable vault handle; mutations run under the session's transition gate
/// so they serialize with unlock and lock.
/// </summary>
[Fody.ConfigureAwait(false)]
internal sealed class VaultIpcHandler(
    IVaultSessionManager sessionManager,
    IVaultWorkspace vaultWorkspace,
    IVaultCipherAttachmentDownloadService attachmentDownloadService) : IVaultClient, IIpcRequestsHandler
{
    public ValueTask<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(
            sessionManager.TryGetUnlockedSession(out var session) ? session.Vault.GetCiphers(query) : []);

    public ValueTask<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(
            sessionManager.TryGetUnlockedSession(out var session) ? session.Vault.GetCipher(request.CipherId) : null);

    [IpcMessageHandler(IpcMessageTypes.Vault.GetFolders)]
    public ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(
            sessionManager.TryGetUnlockedSession(out var session) ? session.Vault.GetFolders() : []);

    [IpcMessageHandler(IpcMessageTypes.Vault.Sync)]
    public async ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default) =>
        await sessionManager.WithSessionAsync(
            async (session, ct) =>
            {
                var outcome = await vaultWorkspace.SyncAsync(
                    session.Account.BitwardenAccountContext,
                    session.UserKey,
                    session.Keys,
                    session.Vault,
                    ct);

                session.ReplaceVault(outcome.Vault);
                return outcome.Result;
            },
            lockedResult: VaultSyncResult.Failed,
            cancellationToken);

    public async ValueTask<VaultCipher?> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        await sessionManager.WithSessionAsync<VaultCipher?>(
            async (session, ct) =>
            {
                var outcome = await vaultWorkspace.SaveCipherAsync(
                    session.Account.BitwardenAccountContext,
                    session.UserKey,
                    session.Vault,
                    request.Cipher,
                    ct);

                session.ReplaceVault(outcome.Vault);
                return outcome.Cipher;
            },
            lockedResult: null,
            cancellationToken);

    public async ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Deliberately outside WithSessionAsync: the download takes the gate itself, for the key
        // derivation only. Wrapping it here would both throw on that inner call and hold the
        // gate — and so block the tray's Lock button — for the length of the transfer.
        var accountContext = sessionManager.GetUnlockedSession().Account.BitwardenAccountContext;

        await attachmentDownloadService.DownloadAsync(accountContext, request, cancellationToken);
    }
}
