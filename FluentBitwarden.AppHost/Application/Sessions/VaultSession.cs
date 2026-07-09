using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Accounts;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Application.Sessions;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSession(
    IAccountUnlockService accountUnlockService,
    IVaultWorkspace vaultWorkspace,
    IAccountStore accountStore,
    SessionStore sessionStore,
    IIpcEventPublisher eventPublisher)
    : IVaultSession
{
    private int _lockGeneration;

    public event Action<VaultSessionStatus>? SessionStatusChanged;

    public bool TryGetUnlockedSession([NotNullWhen(true)] out SessionSnapshot? session) =>
        sessionStore.TryGetSession(out session);

    public SessionSnapshot GetUnlockedSession() => sessionStore.GetSession();

    public async ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken)
    {
        await sessionStore.TransitionGate.WaitAsync(cancellationToken);
        int lockGeneration = Volatile.Read(ref _lockGeneration);
        VaultSessionStatus? statusToPublish = null;
        UserKey? userKey = null;
        KeySession? keys = null;
        try
        {
            var result = accountUnlockService.Unlock(request);
            if (!result.TryGetUserKey(out userKey))
                return result.Outcome;

            var context = new BitwardenAccountContext(request.Account.UserId, request.Account.Environment);
            var keyMaterial = accountStore.GetKeyMaterial(context.UserId)
                ?? throw new InvalidOperationException(
                    $"Account key material not found for user '{context.UserId}'.");
            keys = new KeySession(userKey, keyMaterial.ProtectedPrivateKey);

            var data = await LoadVaultDataAsync(context, userKey, keys, cancellationToken);

            if (accountStore.GetAccountProfileDetails(context.UserId) is null)
                return new AccountUnlockOutcome.Failure(
                    "Account profile details are not available. Connect to Bitwarden and unlock again.");

            // A lock requested while this unlock was in flight wins: abort instead of publishing.
            if (Volatile.Read(ref _lockGeneration) != lockGeneration)
                return new AccountUnlockOutcome.Failure("The vault was locked while unlocking.");

            sessionStore.Swap(new SessionSnapshot(request.Account, userKey, keys, data));
            userKey = null;
            keys = null;
            statusToPublish = VaultSessionStatus.Unlocked;
            return result.Outcome;
        }
        finally
        {
            keys?.Dispose();
            userKey?.Dispose();
            sessionStore.TransitionGate.Release();
            if (statusToPublish is { } status)
                NotifySessionStatusChanged(status);
        }
    }

    private async Task<LoadedVaultData> LoadVaultDataAsync(
        BitwardenAccountContext context,
        UserKey userKey,
        KeySession keys,
        CancellationToken cancellationToken)
    {
        bool forceSync = accountStore.GetAccountProfileDetails(context.UserId) is null;

        var data = vaultWorkspace.Load(userKey, keys);
        if (forceSync || data.CiphersById.Count == 0)
        {
            var syncResult = await vaultWorkspace.SyncAsync(context, userKey, force: true, cancellationToken);
            if (syncResult == VaultSyncResult.Synced)
                data = vaultWorkspace.Load(userKey, keys);
        }

        return data;
    }

    public async void RequestLock()
    {
        try
        {
            await LockAsync();
        }
        catch (Exception exception)
        {
            UnhandledExceptionLogger.WriteException(exception);
        }
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        // Increment before waiting on the gate so an in-flight unlock observes the request and aborts.
        Interlocked.Increment(ref _lockGeneration);

        await sessionStore.TransitionGate.WaitAsync(cancellationToken);
        bool becomeLocked = false;
        try
        {
            becomeLocked = sessionStore.Clear();
        }
        finally
        {
            sessionStore.TransitionGate.Release();
            if (becomeLocked)
                NotifySessionStatusChanged(VaultSessionStatus.Locked);
        }
    }

    private void NotifySessionStatusChanged(VaultSessionStatus status)
    {
        SessionStatusChanged?.Invoke(status);
        _ = eventPublisher.PublishAsync(new VaultSessionStatusChangedEvent(status));
    }
}
