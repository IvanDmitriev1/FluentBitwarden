using FluentBitwarden.AppHost.Modules.Accounts;
using FluentBitwarden.AppHost.Modules.Accounts.KeyManagement;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.AppHost.Modules.Vault.Attachments;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Application.Sessions;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionCoordinator(
    IAccountUnlockService accountUnlockService,
    IVaultWorkspace vaultWorkspace,
    IAccountStore accountStore,
    IAccountKeyService accountKeyService,
    IVaultCipherAttachmentDownloadService attachmentDownloadService,
    IIpcEventPublisher eventPublisher)
    : IVaultSessionCoordinator
{
    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private UnlockedSession? _unlockedSession;

    public event Action<VaultSessionStatus>? SessionStatusChanged;

    public bool TryGetUnlockedSession([NotNullWhen(true)] out UnlockedSession? session)
    {
        session = Volatile.Read(ref _unlockedSession);
        return session is not null;
    }

    public UnlockedSession GetUnlockedSession() => !TryGetUnlockedSession(out var session)
        ? throw new InvalidOperationException("No unlocked account is present")
        : session;

    public async ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken)
    {
        await _transitionGate.WaitAsync(cancellationToken);
        try
        {
            var result = accountUnlockService.Unlock(request);
            if (!result.TryGetUserKey(out var userKey))
                return result.Outcome;

            var context = new BitwardenAccountContext(request.Account.UserId, request.Account.Environment);
            bool forceSync = accountStore.GetAccountProfileDetails(context.UserId) is null;

            var keyMaterial = accountStore.GetKeyMaterial(context.UserId)
                ?? throw new InvalidOperationException(
                    $"Account key material not found for user '{context.UserId}'.");
            accountKeyService.BeginSession(userKey, keyMaterial.ProtectedPrivateKey);

            await vaultWorkspace.OpenAsync(
                context,
                userKey,
                forceSync,
                cancellationToken);

            if (accountStore.GetAccountProfileDetails(context.UserId) is null)
            {
                accountKeyService.EndSession();
                return new AccountUnlockOutcome.Failure(
                    "Account profile details are not available. Connect to Bitwarden and unlock again.");
            }

            PublishSessionChange(new UnlockedSession(request.Account, userKey));
            return result.Outcome;
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    public ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        var session = GetUnlockedSession();
        return new ValueTask(attachmentDownloadService.DownloadAsync(
            session.Account.BitwardenAccountContext,
            request,
            cancellationToken));
    }

    public void RequestLock() => _ = RequestLockAsync();

    public async ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken)
    {
        await _transitionGate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetUnlockedSession(out var session))
                return VaultSyncResult.Failed;

            var result = await vaultWorkspace.SyncAsync(
                session.Account.BitwardenAccountContext,
                cancellationToken: cancellationToken);

            return result;
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    public async ValueTask<VaultCipher?> SaveCipherAsync(VaultCipher cipher, CancellationToken cancellationToken)
    {
        await _transitionGate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetUnlockedSession(out var session))
                return null;

            return await vaultWorkspace.SaveCipherAsync(
                session.Account.BitwardenAccountContext,
                cipher,
                cancellationToken);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task RequestLockAsync(CancellationToken cancellationToken = default)
    {
        await _transitionGate.WaitAsync(cancellationToken);

        try
        {
            if (!TryGetUnlockedSession(out _))
                return;

            vaultWorkspace.Close();
            accountKeyService.EndSession();
            PublishSessionChange(null);

            GC.Collect();
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private void PublishSessionChange(UnlockedSession? session)
    {
        var previousSession = Interlocked.Exchange(ref _unlockedSession, session);
        previousSession?.Dispose();

        VaultSessionStatus status = session is not null ? VaultSessionStatus.Unlocked : VaultSessionStatus.Locked;

        SessionStatusChanged?.Invoke(status);
        _ = eventPublisher.PublishAsync(new VaultSessionStatusChangedEvent(status));
    }
}
