using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Vault.Attachments;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

/// <summary>
/// In-process <see cref="IVaultClient"/> over the unlocked session. Data mutations run under the
/// store's transition gate so they serialize with unlock/lock.
/// </summary>
[Fody.ConfigureAwait(false)]
internal sealed class VaultIpcHandler(
    SessionStore sessionStore,
    IVaultWorkspace vaultWorkspace,
    IVaultCipherAttachmentDownloadService attachmentDownloadService) : IVaultClient, IIpcRequestsHandler
{
    public ValueTask<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(sessionStore.GetCiphers(query));

    public ValueTask<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(sessionStore.GetCipher(request.CipherId));

    [IpcMessageHandler(IpcMessageTypes.Vault.GetFolders)]
    public ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(sessionStore.GetFolders());

    [IpcMessageHandler(IpcMessageTypes.Vault.Sync)]
    public async ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default)
    {
        await sessionStore.TransitionGate.WaitAsync(cancellationToken);
        try
        {
            if (!sessionStore.TryGetSession(out var session))
                return VaultSyncResult.Failed;

            var result = await vaultWorkspace.SyncAsync(
                session.Account.BitwardenAccountContext,
                session.UserKey,
                force: false,
                cancellationToken);

            if (result == VaultSyncResult.Synced)
                sessionStore.ReplaceData(session, vaultWorkspace.Load(session.UserKey, session.Keys));

            return result;
        }
        finally
        {
            sessionStore.TransitionGate.Release();
        }
    }

    public async ValueTask<VaultCipher?> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        await sessionStore.TransitionGate.WaitAsync(cancellationToken);
        try
        {
            if (!sessionStore.TryGetSession(out var session))
                return null;

            var savedCipher = await vaultWorkspace.SaveCipherAsync(
                session.Account.BitwardenAccountContext,
                session.UserKey,
                request.Cipher,
                cancellationToken);

            var ciphersById = new Dictionary<CipherId, VaultCipher>(session.Data.CiphersById)
            {
                [savedCipher.Id] = savedCipher,
            };
            sessionStore.ReplaceData(session, session.Data with { CiphersById = ciphersById });

            return savedCipher;
        }
        finally
        {
            sessionStore.TransitionGate.Release();
        }
    }

    public async ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = sessionStore.GetSession();
        await attachmentDownloadService.DownloadAsync(
            session.Account.BitwardenAccountContext,
            session.Keys,
            request,
            cancellationToken);
    }
}
