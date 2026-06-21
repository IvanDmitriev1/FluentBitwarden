using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Application.Sessions;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionCoordinator(
    IAccountUnlockService accountUnlockService,
    IVaultWorkspace vaultWorkspace)
    : IVaultSessionCoordinator, IBitwardenEnvironmentAccessor
{
    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private UnlockedSession? _unlockedSession;

    public BitwardenEnvironment CurrentEnvironment =>
        Volatile.Read(ref _unlockedSession)?.Account.Environment ??
        throw new InvalidOperationException("No unlocked account is present");

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

            var nextSession = new UnlockedSession(request.Account, userKey);
            var previousSession = ClearUnlockedSession();
            vaultWorkspace.Close();

            try
            {
                await vaultWorkspace.OpenAsync(nextSession.UserKey, cancellationToken);
                PublishUnlockedSession(nextSession);
                return result.Outcome;
            }
            catch
            {
                vaultWorkspace.Close();
                nextSession.Dispose();
                throw;
            }
            finally
            {
                previousSession?.Dispose();
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

            return await vaultWorkspace.SyncAsync(session.UserKey, cancellationToken: cancellationToken);
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
                var session = ClearUnlockedSession();
                vaultWorkspace.Close();
                session?.Dispose();
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

    private UnlockedSession? ClearUnlockedSession() =>
        Interlocked.Exchange(ref _unlockedSession, null);

    private void PublishUnlockedSession(UnlockedSession session) =>
        Interlocked.Exchange(ref _unlockedSession, session)?.Dispose();
}
