using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Application.Sessions;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionCoordinator(
    IAccountUnlockService accountUnlockService,
    IVaultWorkspace vaultWorkspace,
    IUiProcessLauncher uiProcessLauncher)
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

            var newSession = new UnlockedSession(request.Account, userKey);

            try
            {
                await vaultWorkspace.OpenAsync(
                    newSession.AccountContext,
                    newSession.UserKey,
                    cancellationToken);

                PublishSessionChange(newSession);
                return result.Outcome;
            }
            catch
            {
                newSession.Dispose();
                throw;
            }
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    public void RequestLock() => _ = LockAsync();

    public async ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken)
    {
        await _transitionGate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetUnlockedSession(out var session))
                return VaultSyncResult.Failed;

            return await vaultWorkspace.SyncAsync(
                session.AccountContext,
                session.UserKey,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private async Task LockAsync()
    {
        try
        {
            await _transitionGate.WaitAsync();
            try
            {
                uiProcessLauncher.Exit();
                vaultWorkspace.Close();

                PublishSessionChange(null);
            }
            finally
            {
                _transitionGate.Release();
            }
        }
        catch (Exception exception)
        {
            UnhandledExceptionLogger.WriteException(exception);
        }
    }

    private void PublishSessionChange(UnlockedSession? session)
    {
        var previousSession = Interlocked.Exchange(ref _unlockedSession, session);
        previousSession?.Dispose();

        VaultSessionStatus status = session is not null ? VaultSessionStatus.Unlocked : VaultSessionStatus.Locked;
        SessionStatusChanged?.Invoke(status);
    }
}
